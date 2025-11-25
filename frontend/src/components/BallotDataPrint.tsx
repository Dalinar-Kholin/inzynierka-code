interface IBallotDataPrint {
    authSerial : string | null;
    voteSerial : string | null;
    authCode : string | null;

}

export default function BallotDataPrint({authSerial, authCode, voteSerial} : IBallotDataPrint) {

    const checkNull = (s : string | null) => {
        return s !== null && s !== ""
    }

    return (
        <>
            { checkNull(authSerial) ? <p>AuthSerial := {authSerial}</p> : <></>}
            { checkNull(voteSerial) ? <p>VoteSerial := {voteSerial}</p> : <></>}
            { checkNull(authCode) ? <p>AuthCode := {authCode}</p> : <></>}
        </>
    );
}