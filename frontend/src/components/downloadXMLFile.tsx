import Vote, {serializeVoteToXML} from "../XMLbuilder.ts";
import useBallot from "../context/ballot/useBallot.ts";


export default function DownloadXMLFile() {
    const ballotCtx = useBallot()

    const handleDownload = () => {
        const xmlContent = serializeVoteToXML(
            new Vote(
                ballotCtx.ballot.VOTE_SERIAL,
                ballotCtx.ballot.AUTH_CODE,
                ballotCtx.ballot.AUTH_SERIAL,
                ballotCtx.ballot.SELECTED_CODE,
                ballotCtx.ballot.VOTE_SERIAL,
            )
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