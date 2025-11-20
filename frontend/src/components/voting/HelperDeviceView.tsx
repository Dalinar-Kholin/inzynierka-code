import GetBallots from "../getBallots.tsx";
import BallotDataPirnt from "../BallotDataPirnt.tsx";
import GetVoteStatus from "../getVoteStatus.tsx";

export default function HelperDeviceView(){



    return(
        <>
            <BallotDataPirnt />
            <p></p>
            <GetBallots/>
            <GetVoteStatus />
        </>
    )
}