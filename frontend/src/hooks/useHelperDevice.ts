import {useCallback, useEffect, useState} from "react";
import {useStatusMessages} from "./useAlertMessage.ts";
import useGetServerPubKey from "./useGetServerPubKey.ts";
import useGetFrontendKey from "./useGetFrontendKey.ts";
import useGetVotingPackage from "../api/getVotingPackage.ts";



const createContent = (sign : string, pubKey: string) =>{
    return `<?xml version="1.0" encoding="UTF-8"?><Gime><Ballot>${sign}</Ballot><Key>${pubKey}</Key></Gime>`
}



export function useHelperDevice() {
    const [voteSerial, setVoteSerial] = useState<string>("")
    const [authSerial, setAuthSerial] = useState<string>("")
    const [voteCodes, setVoteCodes] = useState<string[]>([])

    const {successMessage, errorMessage, showError, showSuccess, clearMessages} = useStatusMessages()
    const { pubKey } = useGetServerPubKey()
    const {publicKey, setPublicKey} = useGetFrontendKey()
    const [content, setContent] = useState<string>("")
    const {getPackage} = useGetVotingPackage()

    useEffect(() => { // load Key
        setContent(createContent(pubKey, publicKey))
    }, [pubKey]);


    const GetBallot =
        useCallback(async (sign: string) => {const data = await getPackage({sign: sign}).catch(e => showError(e.message))
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
        setPublicKey,
        showError,showSuccess
    }
}