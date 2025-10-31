import { useMemo } from "react";
import {
    useConnection,
    useAnchorWallet, // <-- zamiast useWallet
    type AnchorWallet
} from "@solana/wallet-adapter-react";
import { AnchorProvider, Program } from "@coral-xyz/anchor";
import idl from "../counter.json";
import Commitments from "./commitments";
import type {Counter} from "../counter.ts";

export function YourComponent() {
    const { connection } = useConnection();
    const anchorWallet = useAnchorWallet();

    const program = useMemo<Program<Counter> | null>(() => {
        if (!connection || !anchorWallet) return null;

        const provider = new AnchorProvider(connection, anchorWallet as AnchorWallet, {
            commitment: "confirmed",
        });

        return new Program<Counter>(idl as Counter, provider);
    }, [connection, anchorWallet?.publicKey]);

    if (!anchorWallet) {
        return <div>Connect wallet to continue…</div>;
    }

    if (!program) {
        return <div>Initializing program…</div>;
    }

    return (
        <div>
            <Commitments program={program} />
        </div>
    );
}