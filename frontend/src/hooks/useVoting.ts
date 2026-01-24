import { useCallback, useEffect, useState } from "react";
import { useAnchor } from "./useAnchor.ts";
import getVoteCode from "../api/getVoteCode.ts";
import { useStatusMessages } from "./useAlertMessage.ts";
import useGetServerPubKey from "./useGetServerPubKey.ts";
import useGetAuthCode, { type IOtResponse } from "../api/getAuthCode.ts";
import usePingServerForAcceptVote from "../api/pingServerForAcceptVote.ts";
import useCastVoteCode from "../api/castVote.ts";
import { createCommitment, sha256Hex } from "../helpers/pedersonCommitments.ts";
import { sha256 } from "@noble/hashes/sha2";

function useVoting() {
    const [otPack, setOtPack] = useState<IOtResponse | undefined>(undefined);
    const [voteSerial, setVoteSerial] = useState<string>("")
    const [authSerial, setAuthSerial] = useState<string>("")
    const [voteCodes, setVoteCodes] = useState<string[]>([])
    const [selectedCode, setSelectedCode] = useState<string>("")
    const [voterSign, setVoterSign] = useState<string>("")
    const [serverSign, setServerSign] = useState<number[]>([])
    const { pubKey } = useGetServerPubKey()
    const [accessCode, setAccessCode] = useState<string | undefined>()
    const [bit, setBit] = useState<boolean | undefined>(undefined);
    const [lockCode, setLockCode] = useState<string>("")
    const [commitment, setCommitment] = useState<string>("")

    const { getProgram, getProvider } = useAnchor();
    const { successMessage, errorMessage, showError, showSuccess } = useStatusMessages()

    const { getCode } = useGetAuthCode()
    const { ping } = usePingServerForAcceptVote()
    const { castVote } = useCastVoteCode()

    useEffect(() => {
        (async () => {
            if (otPack?.r && lockCode !== "") {
                setCommitment(await sha256Hex(sha256(await createCommitment(lockCode, otPack.r) + "")))
            }
        })()
    }, [otPack, lockCode])

    const CastVote = useCallback(async (code: string) => {
        setSelectedCode(code)
        console.log(otPack?.authCode)
        try {
            await castVote({
                voteSerial: voteSerial,
                voteCode: code,
                authCode: otPack?.authCode || "",
                program: getProgram(),
                provider: getProvider(),
                lockCode: lockCode,
                setNewAccessCode: setAccessCode,
            })
            showSuccess("vote casted")
        } catch (err: any) {
            showError(err.message)
        }

    }, [otPack, pubKey])

    const GetVoteCodes = useCallback(async () => {
        const data = await getVoteCode({ permCode: otPack?.permCode || "" })
        setVoteCodes(data[data[0].authSerial === voteSerial ? 0 : 1].voteCodes)
        console.log(data)
    }, [otPack])

    const GetAuthCodes = useCallback(async () => {
        if (authSerial?.length !== 16 || bit === undefined) {
            showError(`Invalid auth serial length := ${authSerial?.length}`)
            return;
        }
        setOtPack(await getCode({ authSerial: authSerial || "", bit }))
    }, [authSerial, bit])

    const PingForAccept = useCallback(async () => {
        try {
            const res = await ping({
                sign: "",
                authCode: otPack?.authCode || "",
                voteSerial: voteSerial || "",
            })
            if (res) {
                showSuccess("server accepted vote")
            }
            else showError("server drop vote")
        }
        catch (e: any) {
            showError(e.message)
        }
    }, [voteSerial, otPack])

    const GetAcceptedBallot = async () => {
        const fetchData = async () => {
            const res = await getProgram().account.vote.all()
            const decoder = new TextDecoder("utf-8");
            const item = res
                .filter(a =>
                    decoder.decode(new Uint8Array(a.account.authSerial)) === authSerial
                ).pop()
            if (item !== undefined) {
                setAuthSerial(decoder.decode(new Uint8Array(item.account.authSerial)))
                setVoteSerial(decoder.decode(new Uint8Array(item.account.voteSerial)))
                setSelectedCode(decoder.decode(new Uint8Array(item.account.voteCode)))
                setOtPack({ authCode: decoder.decode(new Uint8Array(item.account.authCode)) })
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
        otPack,
        successMessage,
        errorMessage,
        selectedCode,
        voterSign,
        serverSign,
        accessCode,
        lockCode,
        commitment,
        // funckcje
        setAuthSerial,
        setVoteSerial,
        setSelectedCode,
        setBit,
        setVoterSign,
        setAccessCode,
        setLockCode,

        CastVote,
        GetVoteCodes,
        GetAuthCodes,
        PingForAccept,
        GetAcceptedBallot
    }
}

export default useVoting