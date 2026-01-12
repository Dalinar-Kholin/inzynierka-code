import {useCallback, useEffect, useState} from "react";
import {useStatusMessages} from "./useAlertMessage.ts";
import useGetServerPubKey from "./useGetServerPubKey.ts";
import useGetFrontendKey from "./useGetFrontendKey.ts";
import useGetVotingPackage, {type VotingPack} from "../api/getVotingPackage.ts";



const createContent = (sign : string, pubKey: string) =>{
    return `<?xml version="1.0" encoding="UTF-8"?><Gime><Ballot>${sign}</Ballot><Key>${pubKey}</Key></Gime>`
}



export function useHelperDevice() {
    const [votePack, setVotePack] = useState<VotingPack | null>(null)

    const {successMessage, errorMessage, showError, showSuccess, clearMessages} = useStatusMessages()
    const { pubKey } = useGetServerPubKey()
    const {publicKey, setPublicKey} = useGetFrontendKey()
    const [content, setContent] = useState<string>("")
    const {getPackage} = useGetVotingPackage()

    useEffect(() => { // load Key
        setContent(createContent(pubKey, publicKey))
    }, [pubKey]);


    const GetBallot =
        useCallback(async (sign: string) => {
            try {
                const data = await getPackage({sign: sign})
                setVotePack(data)
                clearMessages()
            }
            catch (error: any) {
                console.log(error)
                showError(error?.message)
            }
        }, [content])


    return {
        votePack,
        successMessage,
        errorMessage,
        content,
        GetBallot,
        setPublicKey,
        showError,showSuccess
    }
}