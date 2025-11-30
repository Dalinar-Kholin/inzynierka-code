import {web3} from "@coral-xyz/anchor";
import {consts} from "../const.ts";
import fetchWithAuth, {IsBadSignError, IsServerError} from "../helpers/fetchWithVerify.ts";


export interface ISignTransaction {
    transaction: string;
    key: string;
}

/*
* transaction is encoded to base64
* transaction := tx.serialize({ requireAllSignatures: false }).toString("base64");
* */

export default async function SignTransaction({transaction, key}: ISignTransaction) {
    const response = await fetchWithAuth<ISignTransaction>(consts.SIGNER_URL + "/sign", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ transaction: transaction }),
    }, key)

    if (IsBadSignError(response)) {
        throw new Error(`bad signed request, server is probably try to cheat`)
    }
    if (IsServerError(response)) {
        throw new Error(response.error);
    }

    return web3.Transaction.from(
        Buffer.from(response.transaction, "base64")
    );
}