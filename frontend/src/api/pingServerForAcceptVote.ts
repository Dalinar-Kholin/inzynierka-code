import {consts} from "../const.ts";

interface IVotingPack {
    authCode: string;
    voteSerial: string;
    sign: string;
}

export default async function pingServerForAcceptVote({ sign, authCode, voteSerial }: IVotingPack): Promise<boolean> {
    const response = await fetch(consts.API_URL + '/acceptVote', {
        method: 'POST',
        body: JSON.stringify({
            basedSign: sign,
            authCode: authCode,
            voteSerial: voteSerial
        }),
    })
    const data = await response.json()
    if(!response.ok){
        throw new Error(data.error);
    }
    return true
}