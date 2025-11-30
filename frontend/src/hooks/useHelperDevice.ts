import {useCallback, useEffect, useState} from "react";
import getVotingPackage from "../api/getVotingPackage.ts";
import {useStatusMessages} from "./useAlertMessage.ts";
import useGetServerPubKey from "./useGetServerPubKey.ts";



const createContent = (c : string) =>{
    return `<?xml version="1.0" encoding="UTF-8"?><Gime><Ballot>${c}</Ballot></Gime>`
}



export function useHelperDevice() {
    const [voteSerial, setVoteSerial] = useState<string>("")
    const [authSerial, setAuthSerial] = useState<string>("")
    const [voteCodes, setVoteCodes] = useState<string[]>([])

    const {successMessage, errorMessage, showError, showSuccess, clearMessages} = useStatusMessages()
    const { pubKey } = useGetServerPubKey()

    const [content, setContent] = useState<string>("")

    useEffect(() => { // load Key
        setContent(createContent(pubKey))
    }, [pubKey]);


    const GetBallot =
        useCallback(async (sign: string) => {const data = await getVotingPackage({sign: sign, key: pubKey}).catch(e => showError(e.message))
            if (data === undefined) {
                return
            }

            setAuthSerial(data.authSerial)
            setVoteSerial(data.voteSerial)
            setVoteCodes(data.voteCodes)
            clearMessages()
        }, [content])


    return {
        voteSerial,
        authSerial,
        voteCodes,
        successMessage,
        errorMessage,
        content,
        GetBallot,

        showError,showSuccess
    }
}