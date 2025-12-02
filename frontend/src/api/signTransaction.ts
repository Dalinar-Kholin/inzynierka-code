import {web3} from "@coral-xyz/anchor";
import {consts} from "../const.ts";
import {type BadSignError, IsBadSignError, IsServerError, type ServerError} from "../helpers/fetchWithVerify.ts";
import type {Transaction} from "@solana/web3.js";
import useFetchWithVerify from "../helpers/fetchWithVerify.ts";


export interface ISignTransaction {
    transaction: string;
    accessCode: string | undefined;
    authCode: string | undefined;
}

interface IResponse {
    transaction : string;
    accessCode: string;
}

export interface ISignTransactionResponse {
    transaction : Transaction;
    newAccessCode: string;
}

interface ISignTransactionRequestWithAuth {
    transaction: string,
    authCode: string,
}

interface ISignTransactionRequestWithAccess {
    transaction: string,
    accessCode: string,
}

type SignRequest = ISignTransactionRequestWithAuth | ISignTransactionRequestWithAccess;

export default function useSignTransaction(){
    const {fetchWithAuth} = useFetchWithVerify()

    async function sign({transaction, accessCode, authCode}: ISignTransaction): Promise<ISignTransactionResponse>{
        return await SignTransaction({transaction, accessCode, authCode, fetchWithAuth})
    }

    return {
        sign,
    }
}

export interface ISignTransactionHelper {
    transaction: string;
    accessCode: string | undefined;
    authCode: string | undefined;
    fetchWithAuth: <T, E>(url: string, options: RequestInit, body: E) => Promise<ServerError | BadSignError | T>
}

async function SignTransaction({transaction, accessCode, authCode, fetchWithAuth}: ISignTransactionHelper): Promise<ISignTransactionResponse> {

    const response = await fetchWithAuth<IResponse, SignRequest>(consts.SIGNER_URL + "/sign", {
        method: "POST",
        headers: { "content-type": "application/json" },
    }, authCode === undefined ? {
        transaction: transaction,
        accessCode: accessCode} as ISignTransactionRequestWithAccess
        : {
            transaction: transaction,
            authCode: authCode,
        } as ISignTransactionRequestWithAuth)

    if (IsBadSignError(response)) {
        throw new Error(`bad signed request, server is probably try to cheat`)
    }
    if (IsServerError(response)) {
        throw new Error(response.error);
    }

    return {
        transaction: web3.Transaction.from(Buffer.from(response.transaction, "base64")),
        newAccessCode: response.accessCode,
    }
}