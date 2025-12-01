import {type ChangeEvent, useState} from "react";
import {Button} from "@mui/material";
import commitVote from "../api/commitVote.ts";
import {useAnchor} from "../hooks/useAnchor.ts";
import useGetServerPubKey from "../hooks/useGetServerPubKey.ts";

interface IUploadSignedDocument{
    voterSign : string;
    authCode : string;
    setVoterSign: (s : string) => void;
    accessCode : string;
}

const handleFileChange = (e: ChangeEvent<HTMLInputElement>, setVoterSign: (s : string) => void) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => {
        const text = String(reader.result ?? "");
        setVoterSign(text);
    };
    reader.readAsText(file);
};

export default function UploadSignedVote({voterSign, setVoterSign, authCode, accessCode} : IUploadSignedDocument) {
    const {getProgram, getProvider} = useAnchor()
    const {pubKey} = useGetServerPubKey()
    return <>
        <h2>upload signed Vote</h2>

        <input type="file" onChange={e => handleFileChange(e ,setVoterSign)}/>

        <pre style={{marginTop: 16, maxHeight: 200, overflow: "auto"}}>
        {voterSign}
      </pre>
        <Button onClick={async () => commitVote({
            signedDocument: voterSign,
            authCode: authCode,
            program: getProgram(),
            provider: getProvider(),
            accessCode: accessCode,
            key: pubKey,
        })}>commit Vote</Button>
    </>
}

interface IUploadSignedVote{
    GetBallot: (s : string) => Promise<void>;
}

export function UploadSignedVoteRequest({GetBallot}: IUploadSignedVote) {
    const [voterSign, setVoterSign] = useState<string>("")

    return <>
        <h2>upload signed Request</h2>

        <input type="file" onChange={e => handleFileChange(e, setVoterSign)}/>

        <pre style={{marginTop: 16, maxHeight: 200, overflow: "auto"}}>
        {voterSign}
      </pre>
        <Button onClick={async () => await GetBallot(voterSign)}>Get Ballot</Button>
    </>
}