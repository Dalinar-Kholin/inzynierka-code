package verify

import (
	"crypto/sha256"
	"crypto/x509"
	"encoding/base64"
	"encoding/hex"
	"encoding/xml"
	"errors"
	"fmt"
	"os"
	"strings"

	"github.com/beevik/etree"
	dsig "github.com/russellhaering/goxmldsig"
	sigtypes "github.com/russellhaering/goxmldsig/types"
)

type Vote struct {
	XMLName    xml.Name `xml:"vote"`
	VoteSerial string   `xml:"VoteSerial"`
	VoteCode   string   `xml:"VoteCode"`
	AuthSerial string   `xml:"AuthSerial"`
	AuthCode   string   `xml:"AuthCode"`
	ServerSign string   `xml:"ServerSign"`

	// ds:Signature w przestrzeni http://www.w3.org/2000/09/xmldsig#
	Signature *sigtypes.Signature `xml:"http://www.w3.org/2000/09/xmldsig# Signature"`
}

func (v *Vote) Verify() error {

	return nil
}

// VerifyVoteSignature sprawdza:
// 1) czy DigestValue w obu Reference są poprawne,
// 2) czy SignatureValue (RSA-SHA256) jest poprawne dla SignedInfo.
// Zwraca użyty certyfikat oraz błąd (nil = podpis kryptograficznie poprawny).
func VerifyVoteSignature(xmlBytes []byte) (*x509.Certificate, error) {
	// 1. Parsowanie do etree – potrzebne do kanonikalizacji
	doc := etree.NewDocument()
	if err := doc.ReadFromBytes(xmlBytes); err != nil {
		return nil, fmt.Errorf("read xml: %w", err)
	}
	root := doc.Root()
	if root == nil || root.Tag != "vote" {
		return nil, errors.New("root element is not <vote>")
	}

	// 2. Znajdź element ds:Signature w drzewie
	sigEl := findChildWithPrefix(root, "ds", "Signature")
	if sigEl == nil {
		return nil, errors.New("ds:Signature element not found")
	}

	// 3. Unmarshal do Vote -> Signature *sigtypes.Signature
	var v Vote
	if err := xml.Unmarshal(xmlBytes, &v); err != nil {
		return nil, fmt.Errorf("unmarshal Vote: %w", err)
	}
	if v.Signature == nil || v.Signature.SignedInfo == nil {
		return nil, errors.New("Signature or SignedInfo is nil")
	}

	sig := v.Signature

	// 4. Wyciągnięcie certyfikatu X.509 z KeyInfo
	if sig.KeyInfo == nil || len(sig.KeyInfo.X509Data.X509Certificates) == 0 {
		return nil, errors.New("no X509Certificate in KeyInfo")
	}
	b64Cert := strings.TrimSpace(sig.KeyInfo.X509Data.X509Certificates[0].Data)
	der, err := base64.StdEncoding.DecodeString(b64Cert)
	if err != nil {
		return nil, fmt.Errorf("decode X509Certificate: %w", err)
	}
	cert, err := x509.ParseCertificate(der)
	if err != nil {
		return nil, fmt.Errorf("parse X509Certificate: %w", err)
	}

	// 5. Weryfikacja digestów Reference
	if err := verifyReferenceDigests(sig, root, sigEl); err != nil {
		return nil, fmt.Errorf("reference digest check failed: %w", err)
	}

	// 6. Weryfikacja SignatureValue (RSA-SHA256 nad SignedInfo)
	if err := verifySignatureValue(sig, sigEl, cert); err != nil {
		return nil, fmt.Errorf("signature value check failed: %w", err)
	}

	// W tym miejscu:
	// - digesty Reference się zgadzają,
	// - SignatureValue jest poprawne względem certyfikatu z KeyInfo.
	return cert, nil
}

func verifyReferenceDigests(sig *sigtypes.Signature, voteRoot, sigEl *etree.Element) error {
	canon := dsig.MakeC14N10ExclusiveCanonicalizerWithPrefixList("")

	refs := sig.SignedInfo.References
	if len(refs) == 0 {
		return errors.New("no References in SignedInfo")
	}

	for i, ref := range refs {

		if ref.DigestAlgo.Algorithm != "http://www.w3.org/2001/04/xmlenc#sha256" {
			return fmt.Errorf("ref[%d]: unsupported DigestMethod %s", i, ref.DigestAlgo.Algorithm)
		}

		var dataToDigest []byte

		switch {
		case ref.URI == "": // Twój główny Reference z Filter2 (subtract ds:Signature)
			// kopia <vote>
			voteCopy := voteRoot.Copy()
			// usuń wszystkie ds:Signature (zgodnie z Filter="subtract")
			removeDescendantsWithPrefix(voteCopy, "ds", "Signature")

			c14n, err := canon.Canonicalize(voteCopy)
			if err != nil {
				return fmt.Errorf("ref[%d]: canonicalize main vote: %w", i, err)
			}
			dataToDigest = c14n

		case strings.HasPrefix(ref.URI, "#"):
			id := strings.TrimPrefix(ref.URI, "#")
			target := findElementById(voteRoot, id)
			if target == nil {
				return fmt.Errorf("ref[%d]: element with Id=%q not found", i, id)
			}

			// kopia
			targetCopy := target.Copy()
			// namespaces z przodków oryginału -> na kopię
			addInScopeNamespaces(targetCopy, target)

			c14n, err := canon.Canonicalize(targetCopy)
			if err != nil {
				return fmt.Errorf("ref[%d]: canonicalize Id=%s: %w", i, id, err)
			}
			dataToDigest = c14n

		default:
			return fmt.Errorf("ref[%d]: unsupported URI %q", i, ref.URI)
		}

		h := sha256.Sum256(dataToDigest)
		got := base64.StdEncoding.EncodeToString(h[:])
		want := strings.TrimSpace(ref.DigestValue)

		if got != want {
			return fmt.Errorf("ref[%d]: digest mismatch: got %s, want %s", i, got, want)
		}
	}
	return nil
}
func verifySignatureValue(sig *sigtypes.Signature, sigEl *etree.Element, cert *x509.Certificate) error {
	if sig.SignedInfo.SignatureMethod.Algorithm != string(dsig.RSASHA256SignatureMethod) {
		return fmt.Errorf("unsupported SignatureMethod: %s", sig.SignedInfo.SignatureMethod.Algorithm)
	}

	// znajdź oryginalny ds:SignedInfo w drzewie
	signedInfoEl := sigEl.FindElement("./ds:SignedInfo")
	if signedInfoEl == nil {
		return errors.New("ds:SignedInfo element not found in etree")
	}

	canon := dsig.MakeC14N10ExclusiveCanonicalizerWithPrefixList("")

	// KOPIA + dopięcie namespace’ów z przodków (m.in. xmlns:ds z <ds:Signature>)
	siCopy := signedInfoEl.Copy()
	addInScopeNamespaces(siCopy, signedInfoEl)

	c14nSignedInfo, err := canon.Canonicalize(siCopy)
	if err != nil {
		return fmt.Errorf("canonicalize SignedInfo: %w", err)
	}

	// DODAJ TYM CZASEM:
	if err := os.WriteFile("signedinfo.bin", c14nSignedInfo, 0644); err != nil {
		panic(err)
	}

	fmt.Println("---- C14N SignedInfo ----")
	fmt.Println(string(c14nSignedInfo))
	fmt.Println("---- SHA256(SignedInfo) ----")
	h := sha256.Sum256(c14nSignedInfo)
	fmt.Println(hex.EncodeToString(h[:]))

	// SignatureValue (base64)
	sigValueB64 := strings.TrimSpace(sig.SignatureValue.Data)
	sigBytes, err := base64.StdEncoding.DecodeString(sigValueB64)
	if err != nil {
		return fmt.Errorf("decode SignatureValue: %w", err)
	}

	// RSA-SHA256 nad kanonikalizowanym SignedInfo
	if err := cert.CheckSignature(x509.SHA256WithRSA, c14nSignedInfo, sigBytes); err != nil {
		return fmt.Errorf("invalid RSA signature: %w", err)
	}

	return nil
}

// dziecko o zadanym prefixie i tagu (np. ds:Signature)
func findChildWithPrefix(parent *etree.Element, prefix, tag string) *etree.Element {
	for _, ch := range parent.ChildElements() {
		if ch.Space == prefix && ch.Tag == tag {
			return ch
		}
	}
	return nil
}

// usuń wszystkie potomne elementy o danym prefixie i tagu (np. ds:Signature)
func removeDescendantsWithPrefix(root *etree.Element, prefix, tag string) {
	for {
		found := root.FindElement(".//" + prefix + ":" + tag)
		if found == nil {
			return
		}
		parent := found.Parent()
		if parent == nil {
			return
		}
		parent.RemoveChild(found)
	}
}

// znajdź element z atrybutem Id == id (bez względu na prefix / namespace)
func findElementById(root *etree.Element, id string) *etree.Element {
	if root == nil {
		return nil
	}
	if root.SelectAttrValue("Id", "") == id {
		return root
	}
	for _, ch := range root.ChildElements() {
		if res := findElementById(ch, id); res != nil {
			return res
		}
	}
	return nil
}
func addInScopeNamespaces(dst, orig *etree.Element) {
	existing := map[string]bool{}
	for _, a := range dst.Attr {
		if isXMLNSAttr(a) {
			existing[xmlnsAttrKey(a)] = true
		}
	}

	// idziemy po przodkach oryginalnego węzła (on ma Parent())
	for p := orig; p != nil; p = p.Parent() {
		for _, a := range p.Attr {
			if isXMLNSAttr(a) {
				key := xmlnsAttrKey(a)
				if !existing[key] {
					dst.Attr = append(dst.Attr, a)
					existing[key] = true
				}
			}
		}
	}
}

func isXMLNSAttr(a etree.Attr) bool {
	// xmlns:xades -> Space="xmlns", Key="xades"
	// xmlns       -> Space="",      Key="xmlns"
	if a.Space == "xmlns" {
		return true
	}
	if a.Space == "" && a.Key == "xmlns" {
		return true
	}
	return false
}

func xmlnsAttrKey(a etree.Attr) string {
	if a.Space == "xmlns" {
		return "xmlns:" + a.Key
	}
	return "xmlns" // default namespace
}
