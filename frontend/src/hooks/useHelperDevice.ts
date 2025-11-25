import {useState} from "react";
import getVotingPackage from "../api/getVotingPackage.ts";
import {useStatusMessages} from "./useAlertMessage.ts";

export function useHelperDevice() {
    const [voteSerial, setVoteSerial] = useState<string | undefined>()
    const [authSerial, setAuthSerial] = useState<string | undefined>()
    const [voteCodes, setVoteCodes] = useState<string[] | undefined>()

    const {successMessage, errorMessage, showError, showSuccess} = useStatusMessages()

    const GetBallot = async () => {
        const data = await getVotingPackage({sign: "chuj"}).catch(e => showError(e.message))
        if (data === undefined) {
            return
        }

        setAuthSerial(data.authSerial)
        setVoteSerial(data.voteSerial)
        setVoteCodes(data.voteCodes)
    }


    return {
        voteSerial,
        authSerial,
        voteCodes,
        successMessage,
        errorMessage,

        // action
        GetBallot,

        showError,showSuccess,
    }
}