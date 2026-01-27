import { Alert, Button, Box, Stack, Typography } from "@mui/material";
import BallotDataPrint from "../BallotDataPrint.tsx";
import DownloadXMLFile from "../downloadXMLFile.tsx";
import UploadSignedVote from "../UploadSignedVote.tsx";
import ResponsiveDialog from "../obliviousTransferDialog.tsx";
import useVoting from "../../hooks/useVoting.ts";
import Vote, { serializeVoteToXML } from "../../helpers/XMLbuilder.ts";
import InputForm from "../InputForm.tsx";

export default function VotingDeviceView() {
    const {
        bit,
        authSerial,
        otPack,
        voteSerial,
        voteCodes,
        successMessage,
        errorMessage,
        selectedCode,
        voterSign,
        serverSign,
        accessCode,
        lockCode,
        commitment,
        PingForAccept,
        setVoteSerial,
        setAuthSerial,
        setVoterSign,
        setBit,
        CastVote,
        GetVoteCodes,
        GetAuthCodes,
        setLockCode,
        GetAcceptedBallot,
    } = useVoting();

    const voteXml = serializeVoteToXML(
        new Vote(
            voteSerial || "",
            selectedCode || "",
            authSerial || "",
            otPack?.authCode || "",
            serverSign || []
        )
    );

    const authCodeReady = bit !== undefined;

    return (
        <Stack spacing={2}>

            {/* Messages */}
            {successMessage && <Alert severity="success">{successMessage}</Alert>}
            {errorMessage && <Alert severity="error">{errorMessage}</Alert>}
            {/* Inputs */}
            <Stack spacing={1}>
                <InputForm name="authSerial" fn={setAuthSerial} value={authSerial} />
                <InputForm name="voteSerial" fn={setVoteSerial} value={voteSerial} />
                <InputForm name="lockCode" fn={setLockCode} value={lockCode} />
            </Stack>

            {/* Vote codes */}

            {/* Auth code OT */}
            <Box>
                <Stack spacing={1}>
                    <Typography variant="body2">Auth code Oblivious Transfer</Typography>

                    <ResponsiveDialog setUseFirstAuthCode={(v) => setBit(v)} />

                    {!authCodeReady ? (
                        <Button variant="outlined" color="error">
                            get AuthCode (firstly choose which code to choose)
                        </Button>
                    ) : (
                        <Button onClick={GetAuthCodes}>get AuthCode</Button>
                    )}
                </Stack>
            </Box>

            <Box>
                <Stack spacing={1}>
                    <Button onClick={GetVoteCodes} variant="outlined">
                        get vote codes
                    </Button>

                    <Stack spacing={1}>
                        {voteCodes?.map((code, i) => (
                            <Button key={code} onClick={() => CastVote(code)}>
                                {i} - {code}
                            </Button>
                        ))}
                    </Stack>
                </Stack>
            </Box>

            {/* Server actions */}
            <Box>
                <Stack spacing={1}>
                    <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
                        <Typography variant="body2">ping server to Accept Vote</Typography>
                        <Button onClick={PingForAccept}>AcceptVote</Button>
                    </Stack>

                    <Button onClick={GetAcceptedBallot}>get AcceptedBallot</Button>
                </Stack>
            </Box>

            {/* Ballot preview */}
            <Box>
                <Stack spacing={1}>
                    <BallotDataPrint
                        voteSerial={voteSerial}
                        authSerial={authSerial}
                        authCode={otPack?.authCode || ""}
                    />

                    {otPack?.r && <Typography variant="body2">{otPack.r}</Typography>}
                </Stack>
            </Box>

            {/* XML + upload signed vote */}
            <Box>
                <Stack spacing={1}>
                    <DownloadXMLFile
                        content={voteXml}
                        filename="vote.xml"
                        name="DownloadXMLVoteFile"
                    />

                    <UploadSignedVote
                        setVoterSign={setVoterSign}
                        authCode={otPack?.authCode || ""}
                        voterSign={voterSign || ""}
                        accessCode={accessCode || ""}
                    />

                    {commitment && <Typography variant="body2">{commitment}</Typography>}
                </Stack>
            </Box>
        </Stack>
    );
}
