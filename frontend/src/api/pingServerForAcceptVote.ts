import {consts} from "../const.ts";

interface IVotingPack {
    authSerial: string;
    sign: string;
}

export default async function pingServerForAcceptVote({ sign, authSerial }: IVotingPack): Promise<void> {
    return fetch(consts.API_URL + '/acceptVote', {
        method: 'POST',
        body: JSON.stringify({ basedSign: sign, authSerial }),
    }).then(response => {
        if (!response.ok) {
            console.log("essa")
        }
        return response.json();
    })
}