import GetBallots from "../getBallots.tsx";
import BallotDataPirnt from "../BallotDataPirnt.tsx";
import GetVoteStatus from "../getVoteStatus.tsx";
import UploadSignedDocument from "../UploadSignedDocument.tsx";
import {useEffect} from "react";
import {useAnchor} from "../../hooks/useAnchor.ts";

export default function HelperDeviceView(){
    const {getProgram} = useAnchor()

    useEffect(()=>{
        // todo : mam zcommitowany public key, teraz dodać narzędzie do weryfikacji podpisu servera
        const fetch = async () => {
            console.log(await getProgram().account.signKey.all())
        }
        fetch().then(r => console.log(r))
    }, [])

    return(
        <>
            <BallotDataPirnt />
            <p></p>
            <GetBallots/>
            <GetVoteStatus />
            <UploadSignedDocument />
        </>
    )
}