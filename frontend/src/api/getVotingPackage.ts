import {consts} from "../const.ts";
import useFetchWithVerify, {
    type FetchWithAuthFnType,
    IsBadSignError,
    IsServerError,
} from "../hooks/useFetchWithVerify.ts";

interface IGetVotingPackage{
    sign : string
}

type InnerMapping = Record<string, number>; // np. { "0": 3, "1": 1, "2": 2, "3": 0 }
type OuterMapping = Map<string, InnerMapping>;

export type VotingPack = {
    authSerial: string;
    lockCode: string;
    lockCodeCommitment: string;
    mapping: OuterMapping;
};
interface VotingPackPartEA {
    authSerial: string;
    lockCode: string;
    lockCodeCommitment: string;
    voteSerials: string[2];
}

interface VotingPackPartSGX {
    authSerial: string;
    mapping: InnerMapping;
}

interface IGetVotePackageRequest {
    signedXML: string;
}


export default function useGetVotingPackage(){
    const fetchWithAuth = useFetchWithVerify()

    async function getPackage({ sign } : IGetVotingPackage): Promise<VotingPack>{
        return await (async () => {
            const sgx = await getVotingPackageFromSGX({sign})
            const ea = await getVotingPackageFromEA({sign, fetchWithAuth})
            const map = new Map<string, InnerMapping>
            map.set(sgx[0].authSerial, sgx[0].mapping)
            map.set(sgx[1].authSerial, sgx[1].mapping)
            const res: VotingPack = {
                authSerial: ea.authSerial,
                lockCode: ea.lockCode,
                lockCodeCommitment: ea.lockCodeCommitment,
                mapping: map
            }


            return res
        })();
    }

    return {
        getPackage,
    }
}

interface IGetVotePackageHelperSign {
    sign : string
}

interface IGetVotePackageHelperFetch {
    fetchWithAuth: FetchWithAuthFnType
    sign : string
}


async function getVotingPackageFromSGX({ sign }: IGetVotePackageHelperSign) : Promise<VotingPackPartSGX[]>{
    try{
        const response = await fetch(consts.SGX_URL + '/voter', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: sign
        })
        return await response.json();
    }
    catch(error){
        throw error;
    }
}

async function getVotingPackageFromEA({ sign, fetchWithAuth } : IGetVotePackageHelperFetch) : Promise<VotingPackPartEA>{

    const response = await fetchWithAuth<VotingPackPartEA, IGetVotePackageRequest>(consts.API_URL + '/getVotingPack', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
    }, { signedXML : sign })

    if (IsBadSignError(response)) {
        throw new Error(`bad signed request, server is probably try to cheat`)
    }
    if (IsServerError(response)) {
        throw new Error("server error");
    }
    return response;
}