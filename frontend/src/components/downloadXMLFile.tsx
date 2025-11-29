interface IDownloadXMLFile {
    content: string;
    name: string;
}

export default function DownloadXMLFile( {content, name} : IDownloadXMLFile ) {

    const handleDownload = () => {

        const blob = new Blob([content], { type: "application/xml" });
        const url = URL.createObjectURL(blob);

        const link = document.createElement("a");
        link.href = url;
        link.download = "vote.xml";
        document.body.appendChild(link);
        link.click();
        link.remove();

        URL.revokeObjectURL(url);
    };

    return <button onClick={handleDownload}>{name}</button>;
}