import * as anchor from "@coral-xyz/anchor";
import { AnchorProvider, Program } from "@coral-xyz/anchor";
import { useConnection } from "@solana/wallet-adapter-react";

import idl from "../counter.json";
import type {Counter} from "../counter.ts";
import {PublicKey} from "@solana/web3.js";

export function useAnchor() {
    const { connection } = useConnection();

    // @ts-ignore
    const dummyWallet: anchor.Wallet = {
        publicKey: new PublicKey("11111111111111111111111111111111"), // any pk
        signTransaction: async (tx) => tx,
        signAllTransactions: async (txs) => txs
    };

    const getProvider = () => {
        if (!dummyWallet || !dummyWallet.publicKey || !dummyWallet.signTransaction) {
            throw new Error("Connect a wallet first.");
        }
        return new AnchorProvider(connection, dummyWallet, {
            commitment: "confirmed",
            preflightCommitment: "confirmed",
        });
    };

    const getProgram = (): Program<Counter> => {
        const provider = getProvider();
        return new Program(idl as Counter, provider);
    };

    return { getProvider, getProgram };
}