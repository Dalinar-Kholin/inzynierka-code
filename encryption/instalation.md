# Instalacja żeby działało generowanie kluczy pailliera
pip uninstall tno.mpc.protocols.distributed_keygen tno.mpc.encryption_schemes.paillier tno.mpc.encryption_schemes.shamir tno.mpc.encryption_schemes.utils tno.mpc.communication
pip install "tno.mpc.communication>=4.8,<4.9" "tno.mpc.encryption_schemes.utils>=0.10,<0.11" "tno.mpc.encryption_schemes.paillier>=3.0.1,<3.1" "tno.mpc.encryption_schemes.shamir>=1.3.0,<1.4" "sympy" "tno.mpc.protocols.distributed_keygen"
