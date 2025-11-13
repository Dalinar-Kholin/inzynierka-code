
import type {Program} from "@coral-xyz/anchor";
import type {Counter} from "../../../smartContract/target/types/counter.ts";


interface IGetCastedVote{
    program: Program<Counter>;
}

export default async function getCastedVotes({program} : IGetCastedVote) {
    const all = await program.account.vote.all();

    const rows = all.map(({ account }) => {

        console.log(account);
        const voteUtf8 = new TextDecoder().decode(
            Uint8Array.from(account.voteCode)
        );
        return `${voteUtf8} — (${account.authCode})`;
    });

    return rows;
}