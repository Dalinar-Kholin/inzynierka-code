import * as anchor from "@coral-xyz/anchor";
import { AnchorProvider, Program } from "@coral-xyz/anchor";
import { useConnection, useWallet } from "@solana/wallet-adapter-react";

import idl from "../counter.json";
import type {Counter} from "../counter.ts";

export function useAnchor() {
    const { connection } = useConnection();
    const wallet = useWallet();

    const getProvider = () => {
        if (!wallet || !wallet.publicKey || !wallet.signTransaction) {
            throw new Error("Connect a wallet first.");
        }
        return new AnchorProvider(connection, wallet as unknown as anchor.Wallet, {
            commitment: "confirmed",
            preflightCommitment: "confirmed",
        });
    };

    const getProgram = (): Program<Counter> => {
        const provider = getProvider();
        return new Program(idl as Counter, provider);
    };

    return { getProvider, getProgram, wallet };
}