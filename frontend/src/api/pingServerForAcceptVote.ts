import {consts} from "../const.ts";
import {type BadSignError, IsBadSignError, IsServerError, type ServerError} from "../helpers/fetchWithVerify.ts";
import useFetchWithVerify from "../helpers/fetchWithVerify.ts";

interface IVotingPack {
    authCode: string;
    voteSerial: string;
    sign: string;
}

interface Request{
    sign: string;
    voteSerial: string;
    authCode: string;
}

interface Response{
    code: number
}

export default function usePingServerForAcceptVote(){
    const {fetchWithAuth} = useFetchWithVerify()

    async function ping({ sign, authCode, voteSerial }: IVotingPack): Promise<boolean>{
        return await pingServerForAcceptVote({sign, authCode, voteSerial,fetchWithAuth })
    }

    return {
        ping,
    }
}

interface IVotingPackHelper{
    authCode: string;
    voteSerial: string;
    sign: string;
    fetchWithAuth: <T, E>(url: string, options: RequestInit, body: E) => Promise<ServerError | BadSignError | T>
}

async function pingServerForAcceptVote({ sign, authCode, voteSerial, fetchWithAuth }: IVotingPackHelper): Promise<boolean> {
    const response = await fetchWithAuth<Response, Request>(consts.API_URL + '/acceptVote', {
        method: 'POST',
    }, {
        sign: sign,
        voteSerial: voteSerial,
        authCode: authCode,
    } )
    
    if (IsBadSignError(response)) {
        throw new Error(`bad signed request, server is probably try to cheat`)
    }
    if (IsServerError(response)) {
        throw new Error(response.error);
    }

    return true
}