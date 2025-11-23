import {consts} from "../const.ts";


interface IGetVoteCode {
    voteSerial: string;
}

export default async function getVoteCode({voteSerial} : IGetVoteCode) {
    const response = await fetch(consts.API_URL + '/getVoteCodes', {
        method: 'POST',
        body: JSON.stringify({
            voteSerial: voteSerial
        }),
    })
    const data = await response.json()
    if (!response.ok) {
        throw new Error(data.error);
    }
    return data as string [];
}