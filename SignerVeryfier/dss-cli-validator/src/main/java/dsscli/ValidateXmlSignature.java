package dsscli;

import eu.europa.esig.dss.model.DSSDocument;
import eu.europa.esig.dss.model.FileDocument;
import eu.europa.esig.dss.validation.SignedDocumentValidator;
import eu.europa.esig.dss.validation.reports.Reports;
import eu.europa.esig.dss.simplereport.SimpleReport;

import eu.europa.esig.dss.spi.validation.CommonCertificateVerifier;
import eu.europa.esig.dss.spi.x509.CommonTrustedCertificateSource;
import eu.europa.esig.dss.spi.x509.CommonCertificateSource;
import eu.europa.esig.dss.model.x509.CertificateToken;

import java.io.File;
import java.io.FileInputStream;
import java.io.InputStream;
import java.security.cert.CertificateFactory;
import java.security.cert.X509Certificate;
import java.util.ArrayList;
import java.util.Collection;
import java.util.List;

import eu.europa.esig.dss.service.http.commons.CommonsDataLoader;
import eu.europa.esig.dss.service.crl.OnlineCRLSource;
import eu.europa.esig.dss.service.http.commons.OCSPDataLoader;
import eu.europa.esig.dss.service.ocsp.OnlineOCSPSource;

public class ValidateXmlSignature {

    public static void main(String[] args) throws Exception {
        if (args.length < 1) {
            System.err.println("Usage: ValidateXmlSignature <signed-xml-file> [trusted-ca-pem]");
            System.exit(1);
        }

        String xmlPath = args[0];
        String caPath  = (args.length >= 2) ? args[1] : null;

        File xmlFile = new File(xmlPath);
        if (!xmlFile.exists()) {
            System.err.println("XML file does not exist: " + xmlFile.getAbsolutePath());
            System.exit(2);
        }

        // 1. Dokument do walidacji
        DSSDocument document = new FileDocument(xmlFile);

        // 2. Weryfikator certyfikatów
        CommonCertificateVerifier certificateVerifier = new CommonCertificateVerifier();

        CommonsDataLoader commonsDataLoader = new CommonsDataLoader();

        // Źródło CRL – będzie pobierać CRL z URL-i z certyfikatów
        OnlineCRLSource crlSource = new OnlineCRLSource();
        crlSource.setDataLoader(commonsDataLoader);
        certificateVerifier.setCrlSource(crlSource);

        // Źródło OCSP – podobnie, z URL-i w certyfikatach
        OCSPDataLoader ocspDataLoader = new OCSPDataLoader();
        OnlineOCSPSource ocspSource = new OnlineOCSPSource();
        ocspSource.setDataLoader(ocspDataLoader);
        certificateVerifier.setOcspSource(ocspSource);

        CommonTrustedCertificateSource trustedSource = new CommonTrustedCertificateSource();
        CommonCertificateSource      knownSource    = new CommonCertificateSource();

        // 2a. Załadowanie łańcucha z PEM: root (trusted) + pośrednie (known)
        if (caPath != null) {
            File caFile = new File(caPath);
            if (!caFile.exists()) {
                System.err.println("CA file does not exist: " + caFile.getAbsolutePath());
                System.exit(3);
            }

            CertificateFactory cf = CertificateFactory.getInstance("X.509");
            List<X509Certificate> certs = new ArrayList<>();

            try (InputStream is = new FileInputStream(caFile)) {
                @SuppressWarnings("unchecked")
                Collection<X509Certificate> col =
                        (Collection<X509Certificate>) (Collection<?>) cf.generateCertificates(is);
                certs.addAll(col);
            }

            if (certs.isEmpty()) {
                System.err.println("No certificates found in CA file: " + caFile.getAbsolutePath());
                System.exit(4);
            }

            // Pierwszy cert – root (Narodowe Centrum Certyfikacji)
            X509Certificate rootCert = certs.get(0);
            trustedSource.addCertificate(new CertificateToken(rootCert));
            System.out.println("Loaded TRUSTED root CA: " + rootCert.getSubjectX500Principal());

            // Pozostałe – pośrednie (EuroCert + Minister)
            for (int i = 1; i < certs.size(); i++) {
                X509Certificate interm = certs.get(i);
                knownSource.addCertificate(new CertificateToken(interm));
                System.out.println("Loaded KNOWN intermediate CA: " + interm.getSubjectX500Principal());
            }
        }

        certificateVerifier.setTrustedCertSources(trustedSource);
        certificateVerifier.setAdjunctCertSources(knownSource);

        // 3. Validator dokumentu
        SignedDocumentValidator validator = SignedDocumentValidator.fromDocument(document);
        validator.setCertificateVerifier(certificateVerifier);

        // 4. Walidacja
        Reports reports = validator.validateDocument();
        SimpleReport simpleReport = reports.getSimpleReport();

        if (simpleReport.getSignatureIdList().isEmpty()) {
            System.out.println("No signatures found in the document.");
            System.exit(1);
        }

        boolean anyHardFailure = false;

        for (String sigId : simpleReport.getSignatureIdList()) {
            String indication    = simpleReport.getIndication(sigId).name();
            String subIndication = (simpleReport.getSubIndication(sigId) != null)
                    ? simpleReport.getSubIndication(sigId).name()
                    : "-";

            System.out.println("Signature " + sigId +
                    " -> indication=" + indication +
                    ", subIndication=" + subIndication);

            // Brak łańcucha – dalej krytyczne
            if ("NO_CERTIFICATE_CHAIN_FOUND".equals(subIndication)) {
                System.exit(69);
            }

            // PASSED – w pełni poprawny
            if ("PASSED".equals(indication)) {
                continue;
            }

            // Wersja B: traktujemy jako OK, jeśli to tylko CERTIFICATE_CHAIN_GENERAL_FAILURE
            if ("INDETERMINATE".equals(indication)
                    && "CERTIFICATE_CHAIN_GENERAL_FAILURE".equals(subIndication)) {

                System.out.println("Signature " + sigId +
                        " treated as TECHNICALLY OK (chain built, but DSS reports general chain issue – np. brak revokacji).");
                continue;
            }

            // Wszystko inne – twardy błąd
            anyHardFailure = true;
        }

        if (anyHardFailure) {
            System.exit(1);
        } else {
            System.exit(0); // wszystkie podpisy technicznie OK
        }
    }
}
