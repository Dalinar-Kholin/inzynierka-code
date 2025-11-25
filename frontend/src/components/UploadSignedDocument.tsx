import {type ChangeEvent} from "react";
import {Button} from "@mui/material";
import commitVote from "../api/commitVote.ts";
import {useAnchor} from "../hooks/useAnchor.ts";

interface IUploadSignedDocument{
    voterSign : string;
    authCode : string;
    setVoterSign: (s : string) => void;
}

export default function UploadSignedDocument({voterSign, setVoterSign, authCode} : IUploadSignedDocument) {
    const {getProgram, getProvider} = useAnchor()


    const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (!file) return;

        const reader = new FileReader();
        reader.onload = () => {
            const text = String(reader.result ?? "");
            setVoterSign(text);
        };
        reader.readAsText(file);
    };


    return <>
        <h2>upload signed Vote</h2>

        <input type="file" onChange={handleFileChange}/>

        <pre style={{marginTop: 16, maxHeight: 200, overflow: "auto"}}>
        {voterSign}
      </pre>
        <Button onClick={async () => commitVote({
            signedDocument: voterSign,
            authCode: authCode,
            program: getProgram(),
            provider: getProvider()
        })}>commit Vote</Button>
    </>
}