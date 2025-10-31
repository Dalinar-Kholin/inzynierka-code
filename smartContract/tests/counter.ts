import { Keypair, PublicKey, SystemProgram, Transaction, LAMPORTS_PER_SOL } from "@solana/web3.js";

import * as anchor from "@coral-xyz/anchor";
import {Program, web3} from "@coral-xyz/anchor";
import BN from "bn.js";

describe("pda", () => {

    const provider = anchor.AnchorProvider.env();
    anchor.setProvider(provider);

    const program = anchor.workspace.Counter as Program;
    const index = new BN(Math.floor(Date.now() / 1000));


    it("Create Message Account (separate payer)", async () => {
        const provider = anchor.getProvider() as anchor.AnchorProvider;
        const program = anchor.workspace.Counter as anchor.Program; // dopasuj nazwę
        const userPubkey = provider.wallet.publicKey;

        // 1) Osobny payer
        const payerPubkey = new PublicKey("6zuVDoqf3KZmAWgDaqQK1K7XkmXnDyyPpCJneAYuyky1");

        // 2) Zasil payera (devnet/localnet)
        await provider.connection.confirmTransaction(
            await provider.connection.requestAirdrop(payerPubkey, 2 * LAMPORTS_PER_SOL),
            "confirmed"
        );

        // 3) Wyznacz PDA dla message_account: seeds = ["message", user.key()]
        const [messagePda] = PublicKey.findProgramAddressSync(
            [Buffer.from("message"), userPubkey.toBuffer(), index.toArrayLike(Buffer, "le", 8)],
            program.programId
        );

        const message = "no asdfasdf";

        const ix = await program.methods
            .create(index, message)
            .accounts({
                user: userPubkey,
                payer: payerPubkey,
                messageAccount: messagePda,
                systemProgram: SystemProgram.programId,
            })
            .instruction();


        const { blockhash, lastValidBlockHeight } =
            await provider.connection.getLatestBlockhash("confirmed");

        const tx = new Transaction({
            feePayer: payerPubkey,
            recentBlockhash: blockhash,
        }).add(ix);

        const unsignedBase64 = tx.serialize({ requireAllSignatures: false }).toString("base64");

        const resp = await fetch("http://localhost:8080/sign", {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ transaction: unsignedBase64 }),
        });

        const { transaction: payerSignedBase64 } = await resp.json();

        const txPayerSigned = web3.Transaction.from(
            Buffer.from(payerSignedBase64, "base64")
        );

        const txFullySigned = await provider.wallet.signTransaction(txPayerSigned);

        const sig = await provider.connection.sendRawTransaction(
            txFullySigned.serialize({ requireAllSignatures: true })
        );
        await provider.connection.confirmTransaction(
            { signature: sig, blockhash, lastValidBlockHeight },
            "confirmed"
        );


        const results = await program.account.messageAccount.all([
            { memcmp: { offset: 8, bytes: userPubkey.toBase58() } },
        ]);
        console.log(results)
    });



    it("Update Message Account", async () => {
        const message = "Hello, Solana!";
        const transactionSignature = await program.methods
            .update(message)
            .accounts({
                messageAccount: messagePda,
            })
            .rpc({ commitment: "confirmed" });

        const messageAccount = await program.account.messageAccount.fetch(
            messagePda,
            "confirmed"
        );

        console.log(JSON.stringify(messageAccount, null, 2));
        console.log(
            "Transaction Signature:",
            `https://solana.fm/tx/${transactionSignature}?cluster=devnet-solana`
        );
    });

    it("Delete Message Account", async () => {
        const transactionSignature = await program.methods
            .delete()
            .accounts({
                messageAccount: messagePda,
            })
            .rpc({ commitment: "confirmed" });

        const messageAccount = await program.account.messageAccount.fetchNullable(
            messagePda,
            "confirmed"
        );

        console.log("Expect Null:", JSON.stringify(messageAccount, null, 2));
        console.log(
            "Transaction Signature:",
            `https://solana.fm/tx/${transactionSignature}?cluster=devnet-solana`
        );
    });
});
