import Vote, {serializeVoteToXML} from "../XMLbuilder.ts";

interface IDownloadXMLFile {
    vote: Vote;
}

export default function DownloadXMLFile( {vote} : IDownloadXMLFile ) {

    const handleDownload = () => {
        const xmlContent = serializeVoteToXML(
            vote
        )

        const blob = new Blob([xmlContent], { type: "application/xml" });
        const url = URL.createObjectURL(blob);

        const link = document.createElement("a");
        link.href = url;
        link.download = "vote.xml";
        document.body.appendChild(link);
        link.click();
        link.remove();

        URL.revokeObjectURL(url);
    };

    return <button onClick={handleDownload}>Download generated XML</button>;
}