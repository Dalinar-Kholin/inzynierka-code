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
    const resp : ISignTransaction = await fetch(consts.SIGNER_URL + "/sign", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ transaction: transaction }),
    }).then(res => {
        if (!res.ok){
            throw new Error("Failed to sign transaction bad errorcode");
        }
        return res;
    }).then(
        r => r.json()
    );


    return web3.Transaction.from(
        Buffer.from(resp.transaction, "base64")
    );
}