import asyncio
from typing import List
import json
import base64
from math import factorial

from tno.mpc.communication import Pool
from tno.mpc.protocols.distributed_keygen import DistributedPaillier

corruption_threshold = 2
key_length = 1024
prime_thresh = 2000
correct_param_biprime = 40
stat_sec_shamir = 40

PARTIES = 5

def save_keys_to_files(distributed_paillier_schemes, filename_prefix="paillier_keys"):
    """
    Zapisuje klucze Paillier do plików JSON w czytelnym formacie
    """
    
    scheme = distributed_paillier_schemes[0]
    
    public_key_data = {
        "n": str(scheme.public_key.n),
        "n_squared": str(scheme.public_key.n_squared),
        "g": str(scheme.public_key.g),
        "key_length": key_length,
    }
    
    private_keys_data = {}
    
    for i, server_scheme in enumerate(distributed_paillier_schemes):
        secret_key = server_scheme.secret_key
        private_keys_data[i] = {
            "player_id": secret_key.player_id,
            "n": str(secret_key.n),
            "n_squared": str(scheme.public_key.n_squared),
            "theta_inv": str(secret_key.theta_inv),
            "degree": corruption_threshold * 2,
            "fac_of_parties": factorial(PARTIES),
            "share": str(list(secret_key.share.shares.values())[0]),
        }
    
    with open(f"../keys/{filename_prefix}_public.json", "w") as f:
        json.dump(public_key_data, f, indent=2)
    
    with open(f"../keys/{filename_prefix}_private.json", "w") as f:
        json.dump(private_keys_data, f, indent=2)
    
    return public_key_data, private_keys_data


def setup_local_pool(server_port: int, ports: List[int]) -> Pool:
    pool = Pool()
    pool.add_http_server(server_port)
    for client_port in (port for port in ports if port != server_port):
        pool.add_http_client(f"client{client_port}", "localhost", client_port)
    return pool

local_ports = [3000 + i for i in range(PARTIES)]
local_pools = [
    setup_local_pool(server_port, local_ports) for server_port in local_ports
]

loop = asyncio.get_event_loop()
async_coroutines = [
    DistributedPaillier.from_security_parameter(
        pool,
        corruption_threshold,
        key_length,
        prime_thresh,
        correct_param_biprime,
        stat_sec_shamir,
        distributed=False,
    )
    for pool in local_pools
]
print("Starting distributed key generation protocol.")
distributed_paillier_schemes = loop.run_until_complete(
    asyncio.gather(*async_coroutines)
)

print("The protocol has completed.")
scheme = distributed_paillier_schemes[0]

print("\n=== Saving keys to files ===")
public_key, private_keys = save_keys_to_files(distributed_paillier_schemes)

print("Keys saved to files.")