import {consts} from "../const.ts";
import {
    type FetchWithAuthFnType,
    IsBadSignError,
    IsServerError,
} from "../hooks/useFetchWithVerify.ts";
import useFetchWithVerify from "../hooks/useFetchWithVerify.ts";

interface IGetVotingPackage{
    sign : string
}

export interface VotingPack {
    authSerial: string;
    voteSerial: string;
    voteCodes: string[];
}

interface IGetVotePackageRequest {
    basedSign: string;
}


export default function useGetVotingPackage(){
    const fetchWithAuth = useFetchWithVerify()

    async function getPackage({ sign } : IGetVotingPackage): Promise<VotingPack>{
        return await getVotingPackage({sign, fetchWithAuth})
    }

    return {
        getPackage,
    }
}

interface IGetVotePackageHelper {
    sign : string
    fetchWithAuth: FetchWithAuthFnType
}

async function getVotingPackage({ sign, fetchWithAuth } : IGetVotePackageHelper) : Promise<VotingPack>{

    const response = await fetchWithAuth<VotingPack, IGetVotePackageRequest>(consts.API_URL + '/getVotingPack', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
    }, { basedSign : sign })

    if (IsBadSignError(response)) {
        throw new Error(`bad signed request, server is probably try to cheat`)
    }
    if (IsServerError(response)) {
        throw new Error("server error");
    }
    return response;
}