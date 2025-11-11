solana-test-validator --reset &



cd ./smartContract

sleep 3

anchor build && anchor deploy

solana airdrop 100 6zuVDoqf3KZmAWgDaqQK1K7XkmXnDyyPpCJneAYuyky1
