import {Button} from "@mui/material";
import useBallot from "../context/ballot/useBallot.ts";
import {consts} from "../const.ts";
import DownloadXMLFile from "./downloadXMLFile.tsx";
import UploadSignedDocument from "./UploadSignedDocument.tsx";

export default function SendVote(){
    const ballotCtx = useBallot()


    return (
        <>
            <Button onClick={()=>{
                fetch("http://localhost:8083/verify",{
                    method: "POST",
                    body: JSON.stringify({
                        document : ballotCtx.ballot.VOTER_SIGN,
                    })
                })
            }}>check document</Button>


            <DownloadXMLFile />
            <UploadSignedDocument/>
            <Button onClick={()=>{
                fetch(consts.API_URL + "/test", {
                    method: "POST",
                    headers: { "content-type": "application/json" },
                    body: JSON.stringify({ sign: "", VoteSerial: ballotCtx.ballot.VOTE_SERIAL, AuthCode: ballotCtx.ballot.AUTH_CODE }),
                }).then(res => {
                    if (!res.ok){
                        throw new Error("Failed to sign transaction bad errorcode");
                    }
                    return res;
                }).then(
                    r => r.json()
                );
            }}>Test</Button>
            <div>

            </div>
        </>
    )
}