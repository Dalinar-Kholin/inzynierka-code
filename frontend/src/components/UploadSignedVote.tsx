import {useState} from "react";
import {Button} from "@mui/material";
import {useAnchor} from "../hooks/useAnchor.ts";
import useCommitVote from "../api/commitVote.ts";
import {useFileSystem} from "../hooks/useFileSystem.ts";

interface IUploadSignedDocument{
    voterSign : string;
    authCode : string;
    setVoterSign: (s : string) => void;
    accessCode : string;
}


export default function UploadSignedVote({voterSign, setVoterSign, authCode, accessCode} : IUploadSignedDocument) {
    const {getProgram, getProvider} = useAnchor()
    const {commit} = useCommitVote()
    const {handleFileChange} = useFileSystem()

    return <>
        <h2>upload signed Vote</h2>

        <input type="file" onChange={e =>
            handleFileChange(
                e.target.files?.[0],
                setVoterSign)
        }/>

        <pre style={{marginTop: 16, maxHeight: 200, overflow: "auto"}}>
        {voterSign}
      </pre>
        <Button onClick={async () => await commit({
            signedDocument: voterSign,
            authCode: authCode,
            program: getProgram(),
            provider: getProvider(),
            accessCode: accessCode,
        })}>commit Vote</Button>
    </>
}

interface IUploadSignedVote{
    GetBallot: (s : string) => Promise<void>;
}

export function UploadSignedVoteRequest({GetBallot}: IUploadSignedVote) {
    const [voterSign, setVoterSign] = useState<string>("")
    const {handleFileChange} = useFileSystem()

    return <>
        <h2>upload signed Request</h2>

        <input type="file" onChange={e => handleFileChange(e.target.files?.[0], setVoterSign)}/>

        <pre style={{marginTop: 16, maxHeight: 200, overflow: "auto"}}>
        {voterSign}
      </pre>
        <Button onClick={async () => await GetBallot(voterSign)}>Get Ballot</Button>
    </>
}

interface IUploadContentToState {
    name: string
    setContent : (c : string) => void;
}

export function UploadContentToState({name, setContent}: IUploadContentToState) {
    const {handleFileChange} = useFileSystem()

    return <>
        <h2>{name}</h2>
        <input type="file" onChange={e => handleFileChange(e.target.files?.[0], setContent)}/>
    </>
}