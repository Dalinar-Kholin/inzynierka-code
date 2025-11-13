import {consts} from "../const.ts";

interface IVotingPack {
    authCode: string;
    voteSerial: string;
    sign: string;
}

export default async function pingServerForAcceptVote({ sign, authCode, voteSerial }: IVotingPack): Promise<void> {
    return fetch(consts.API_URL + '/acceptVote', {
        method: 'POST',
        body: JSON.stringify({
            basedSign: sign,
            authCode: authCode,
            voteSerial: voteSerial
        }),
    }).then(response => {
        if (!response.ok) {
            console.log("essa")
        }
        return response.json();
    })
}