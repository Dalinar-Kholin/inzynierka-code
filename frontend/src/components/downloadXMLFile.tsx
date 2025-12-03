import {useFileSystem} from "../hooks/useFileSystem.ts";

interface IDownloadXMLFile {
    content: string;
    name: string;
    filename: string;
}

export default function DownloadXMLFile( {content, filename, name} : IDownloadXMLFile ) {
    const {handleDownload} = useFileSystem();

    return <button onClick={ () => handleDownload(content, filename)}>{name}</button>;
}