import { useEffect, useState } from "react";
import type {IdlAccounts, Program} from "@coral-xyz/anchor";
import type {Counter} from "../counter.ts";
import type {ProgramAccount} from "@coral-xyz/anchor/dist/cjs/program/namespace/account";

type AuthPackCommitment = IdlAccounts<Counter>['packCommitment'];

export default function Commitments({ program }: { program: Program<Counter> | null }) {
    const [list, setList] = useState<ProgramAccount<AuthPackCommitment>[]>([]);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        if (!program) return;
        let cancelled = false;

        (async () => {
            setLoading(true);
            try {
                const all = await program.account.packCommitment.all();
                if (!cancelled) setList(all);
            } finally {
                if (!cancelled) setLoading(false);
            }
        })();

        return () => { cancelled = true; };
    }, [program]);

    if (!program) return <div>Connect wallet</div>;
    if (loading) return <div>Loading…</div>;

    return (
        <ul>
            {list.length}
            {list.map((it) => (
                <li key={it.publicKey.toBase58()}>
                    {Array.from(it.account.hashedData, b =>
                        Number(b).toString(16).padStart(2, '0')
                    ).join('')}
                </li>

            ))}
        </ul>
    );
}