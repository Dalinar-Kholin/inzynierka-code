import {consts} from "../const.ts";
import fetchWithAuth, {IsBadSignError, IsServerError} from "../helpers/fetchWithVerify.ts";

interface IGetVotingPackage{
    sign : string
    key : string
}

export interface VotingPack {
    authSerial: string;
    voteSerial: string;
    voteCodes: string[];
}

export default async function getVotingPackage({ sign, key } : IGetVotingPackage) : Promise<VotingPack>{
    const response = await fetchWithAuth<VotingPack>(consts.API_URL + '/getVotingPack', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ basedSign : sign }),
    }, key)
    if (IsBadSignError(response)) {
        throw new Error(`bad signed request, server is probably try to cheat`)
    }
    if (IsServerError(response)) {
        throw new Error(response.error);
    }
    return response;
}