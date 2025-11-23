import {consts} from "../const.ts";

interface IGetVotingPackage{
    sign : string
}

export interface VotingPack {
    authSerial: string;
    voteSerial: string;
    voteCodes: string[];
    ackCode: string;
    // authCode?: null; // jeśli kiedyś będzie przekazywane osobno
}

export default async function getVotingPackage({ sign } : IGetVotingPackage) : Promise<VotingPack>{
    const request = await fetch(consts.API_URL + '/getVotingPack', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ basedSign : sign }),
    })
    const data = await request.json();
    if (request.ok) {
        return data as VotingPack;
    }

    throw new Error(data.error);
}