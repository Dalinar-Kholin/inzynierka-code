import {useCallback, useState} from "react";
import castVoteCode from "../api/castVote.ts";
import {useAnchor} from "./useAnchor.ts";
import getVoteCode from "../api/getVoteCode.ts";
import getAuthCode from "../api/getAuthCode.ts";
import pingServerForAcceptVote from "../api/pingServerForAcceptVote.ts";
import {useStatusMessages} from "./useAlertMessage.ts";
import {stringify} from "uuid";
import useGetServerPubKey from "./useGetServerPubKey.ts";

function useVoting() {
    const [authCode, setAuthCode] = useState<string>("")
    const [voteSerial, setVoteSerial] = useState<string>("")
    const [authSerial, setAuthSerial] = useState<string>("")
    const [voteCodes, setVoteCodes] = useState<string[]>([])
    const [selectedCode, setSelectedCode] = useState<string>("")
    const [voterSign, setVoterSign] = useState<string>("")
    const [serverSign, setServerSign] = useState<number[]>([])
    const {pubKey} = useGetServerPubKey()
    const [accessCode, setAccessCode] = useState<string | undefined>()
    const [bit, setBit] = useState<boolean | undefined>(undefined);

    const {getProgram, getProvider} = useAnchor();
    const {successMessage, errorMessage, showError, showSuccess} = useStatusMessages()

    const CastVote = useCallback(async (code: string) => {
        setSelectedCode(code)
        try {
            await castVoteCode({
                voteCode: code,
                authCode: authCode || "",
                program: getProgram(),
                provider: getProvider(),
                setNewAccessCode: setAccessCode,
                key: pubKey,
            })
            showSuccess("vote casted")
        }catch(err: any) {
            showError(err.message)
        }

        }, [authCode, pubKey])

    const GetVoteCodes = useCallback(async () => {
        setVoteCodes(await getVoteCode({voteSerial: voteSerial || ""}))
    }, [voteSerial])

    const GetAuthCodes = useCallback(async () => {
            if (authSerial?.length !== 36 || bit === undefined) {
                showError(`Invalid auth serial length := ${authSerial?.length}`)
                return;
            }
            setAuthCode((await getAuthCode({authSerial: authSerial || "", bit, key: pubKey })).result)
        }, [authSerial, bit])

    const PingForAccept = useCallback(async () => {
            try {
                const res =  await pingServerForAcceptVote({
                    sign: "",
                    authCode: authCode || "",
                    voteSerial: voteSerial || ""
                })
                if (res){
                    showSuccess("server accepted vote")
                }
                else showError("server drop vote")
            }
            catch (e: any) {
                showError(e.message)
            }
        }, [voteSerial, authCode])

    const GetAcceptedBallot = async () =>{
        const fetchData = async () => {
            const res = await getProgram().account.vote.all()
            const decoder = new TextDecoder("utf-8");
            const item = res
                .filter(a =>
                    stringify(new Uint8Array(a.account.authSerial)) === authSerial
                ).pop()
            if (item !== undefined) {
                setAuthSerial(stringify(new Uint8Array(item.account.authSerial)))
                setVoteSerial(stringify(new Uint8Array(item.account.voteSerial)))
                setSelectedCode(decoder.decode(new Uint8Array(item.account.voteCode)))
                setAuthCode(decoder.decode(new Uint8Array(item.account.authCode)))
                setServerSign(item.account.serverSign)
                setVoterSign(decoder.decode(new Uint8Array(item.account.voterSign)))

                showSuccess("vote downoladed")
                return
            }
            showError("cant get ballot")
        };
        await fetchData();
    }

    return {
        // wartości
        bit,
        authSerial,
        voteSerial,
        voteCodes,
        authCode,
        successMessage,
        errorMessage,
        selectedCode,
        voterSign,
        serverSign,
        accessCode,
        // funckcje
        setAuthSerial,
        setVoteSerial,
        setSelectedCode,
        setBit,
        setVoterSign,
        setAccessCode,

        CastVote,
        GetVoteCodes,
        GetAuthCodes,
        PingForAccept,
        GetAcceptedBallot
    }
}

export default useVoting