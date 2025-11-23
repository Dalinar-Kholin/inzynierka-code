import {web3} from "@coral-xyz/anchor";
import {consts} from "../const.ts";


export interface ISignTransaction {
    transaction: string;
}

/*
* transaction is encoded to base64
* transaction := tx.serialize({ requireAllSignatures: false }).toString("base64");
* */
export default async function SignTransaction(transaction: string) {
    //todo: podpisać wiadomość
    const response = await fetch(consts.SIGNER_URL + "/sign", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ transaction: transaction }),
    })
    const data = await response.json();
    if (!response.ok) {
        throw new Error(data.error);
    }
    const resp : ISignTransaction = data as ISignTransaction;

    return web3.Transaction.from(
        Buffer.from(resp.transaction, "base64")
    );
}