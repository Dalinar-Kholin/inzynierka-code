import {useContext} from "react";
import {BallotContext} from "../context/ballot/context.tsx";

export default function BallotDataPirnt() {
    const ballotCtx = useContext(BallotContext);

    return (
        <>
            {ballotCtx.ballot.AUTH_SERIAL !== "" ? <p>AuthSerial := {ballotCtx.ballot.AUTH_SERIAL}</p> : <></>}
            {ballotCtx.ballot.VOTE_SERIAL !== "" ? <p>VoteSerial := {ballotCtx.ballot.VOTE_SERIAL}</p> : <></>}
            {ballotCtx.ballot.AUTH_CODE !== "" ? <p>AuthCode := {ballotCtx.ballot.AUTH_CODE}</p> : <></>}
        </>
    );
}