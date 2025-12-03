import {web3} from "@coral-xyz/anchor";
import {consts} from "../const.ts";
import {
    type FetchWithAuthFnType,
    IsBadSignError,
    IsServerError,
} from "../hooks/useFetchWithVerify.ts";
import type {Transaction} from "@solana/web3.js";
import useFetchWithVerify from "../hooks/useFetchWithVerify.ts";




export type ISignTransaction =
    | { transaction: string; accessCode: string; authCode?: undefined }
    | { transaction: string; authCode: string;  accessCode?: undefined }

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

export type useSignTransactionFnType = ReturnType<typeof useSignTransaction>;

export default function useSignTransaction(){
    const fetchWithAuth = useFetchWithVerify()

    return async function sign({transaction, accessCode, authCode}: ISignTransaction): Promise<ISignTransactionResponse>{
        return await SignTransaction({transaction, accessCode, authCode, fetchWithAuth})
    }
}

export interface ISignTransactionHelper {
    transaction: string;
    accessCode: string | undefined;
    authCode: string | undefined;
    fetchWithAuth: FetchWithAuthFnType
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