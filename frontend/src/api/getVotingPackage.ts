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
    return fetch(consts.API_URL + '/getPackage', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ basedSign : sign }),
    }).then(response => {
        if (!response.ok) {
            console.log("essa")
        }
        return response.json();
    })
}