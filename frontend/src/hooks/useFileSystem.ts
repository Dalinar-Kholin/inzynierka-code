export function useFileSystem(){
    const handleFileChange = (e: File | undefined, setState: (s : string) => void) => {
        const file = e;
        if (!file) return;

        const reader = new FileReader();
        reader.onload = () => {
            setState(String(reader.result ?? ""));
        };
        reader.readAsText(file);
    };

    const handleDownload = (content: string, filename: string) => {
        const blob = new Blob([content], { type: "application/xml" });
        const url = URL.createObjectURL(blob);

        const link = document.createElement("a");
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        link.remove();

        URL.revokeObjectURL(url);
    };

    return {handleFileChange, handleDownload};
}