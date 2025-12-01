import {web3} from "@coral-xyz/anchor";
import {consts} from "../const.ts";
import fetchWithAuth, {IsBadSignError, IsServerError} from "../helpers/fetchWithVerify.ts";
import type {Transaction} from "@solana/web3.js";


export interface ISignTransaction {
    transaction: string;
    accessCode: string | undefined;
    authCode: string | undefined;
    key: string;
}

interface IResponse {
    transaction : string;
    accessCode: string;
}

interface ISignTransactionResponse {
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

export default async function SignTransaction({transaction, key, accessCode, authCode}: ISignTransaction): Promise<ISignTransactionResponse> {
    const response = await fetchWithAuth<IResponse, SignRequest>(consts.SIGNER_URL + "/sign", {
        method: "POST",
        headers: { "content-type": "application/json" },
    }, key, authCode === undefined ? {
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