import * as anchor from "@coral-xyz/anchor";
import { AnchorProvider, Program } from "@coral-xyz/anchor";
import { useConnection, useWallet } from "@solana/wallet-adapter-react";

import idl from "../counter.json"; // must contain "metadata.address" (programId)
import type {Counter} from "../counter.ts"; // must contain "metadata.address" (programId)
// must contain "metadata.address" (programId)


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

// Utilities to parse inputs into fixed byte arrays (utf8/hex/base64 allowed)
export function toBytesFixed(
    input: string | Uint8Array,
    expectedLen: number
): Uint8Array {
    let bytes: Uint8Array;
    if (input instanceof Uint8Array) {
        bytes = input;
    } else if (/^[0-9a-fA-F]+$/.test(input) && input.length % 2 === 0) {
        // hex
        const arr = new Uint8Array(input.length / 2);
        for (let i = 0; i < arr.length; i++) {
            arr[i] = parseInt(input.slice(i * 2, i * 2 + 2), 16);
        }
        bytes = arr;
    } else if (/^[A-Za-z0-9+/=]+$/.test(input) && input.includes("=")) {
        // base64 (simple heuristic)
        bytes = Uint8Array.from(atob(input), (c) => c.charCodeAt(0));
    } else {
        // utf8
        bytes = new TextEncoder().encode(input);
    }

    if (bytes.length !== expectedLen) {
        throw new Error(`Input must be exactly ${expectedLen} bytes, got ${bytes.length}`);
    }
    return bytes;
}