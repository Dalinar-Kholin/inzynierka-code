use anchor_lang::prelude::*;

declare_id!("5Me1semZqTJzDwrCR4qsBx1BMX5MfHdvNNqdNAGWBa56");

#[program]
mod counter {
    use super::*;

    pub fn cast_vote( // do umieszczenia krotki <VoteCode,AuthCode> na BB
        ctx: Context<Cast>,
        auth_code: [u8; 64],
        vote_code: [u8; 32],
    ) -> Result<()> {
        let commitment_data = &mut ctx.accounts.cast;
        commitment_data.auth_code = auth_code;
        commitment_data.vote_code = vote_code                                                                               ;
        commitment_data.bump = ctx.bumps.cast;
        Ok(())
    }

    pub fn accept_vote(
        ctx: Context<Accept>,
    ) -> Result<()>{ // do umieszczenia krotki <VoteSerial, VoteCode, AuthSerial, AuthCode, AckCode, sig> na BB
        Ok(())
    }

    pub fn commit_vote(
        ctx: Context<Commit>,
    ) -> Result<()>{ // do umieszczenia krotki <VoteSerial, VoteCode, AuthSerial, AuthCode, AckCode, sig(servera), sig(usera)> na BB
        Ok(())
    }

    pub fn create_commitment_pack(
        ctx: Context<CreateAuthPackCommitment>,
        commitment_type: u8,
        hashed_data: [u8; 32],
    ) -> Result<()> {
        let commitment_data = &mut ctx.accounts.commitment;
        commitment_data.commitment_type = commitment_type;
        commitment_data.hashed_data = hashed_data;
        commitment_data.bump = ctx.bumps.commitment;
        Ok(())
    }
}

#[derive(Accounts)]
#[instruction(commitment_type: u8, hashed_data : [u8; 32])]
pub struct CreateAuthPackCommitment<'info> {
    #[account(mut)]
    pub payer: Signer<'info>,

    #[account(
        init,
        payer = payer,
        space = 32 + 1 + 1 + 8, // todo : poprawnie to policzyć a nie na palcach
        seeds = [b"createPackCommitment".as_ref(), &[commitment_type]],
        bump
    )]
    pub commitment: Account<'info, PackCommitment>,

    pub system_program: Program<'info, System>,
}

#[account]
pub struct PackCommitment {
    pub commitment_type: u8,
    pub hashed_data: [u8; 32],
    pub bump: u8,
}

#[derive(Accounts)]
#[instruction(vote_code: [u8; 32], auth_code: [u8; 32])]
pub struct Cast<'info> {
    #[account(mut)]
    pub payer: Signer<'info>,
    #[account(mut)]
    pub user: Signer<'info>,

    #[account(
        init,
        seeds = [b"castVote".as_ref(), vote_code.as_ref(), auth_code.as_ref()], // todo : poprawnie to policzyć a nie na palcach
        bump,
        payer = payer,
        space = 8 + 32 + 8 + 4 + 64 + 1
    )]
    pub cast: Account<'info, CastVote>,
    pub system_program: Program<'info, System>,
}

#[account]
pub struct CastVote {
    pub vote_code: [u8; 32],
    pub auth_code: [u8; 64],
    pub bump: u8,
}

#[derive(Accounts)]
#[instruction(vote_serial: [u8; 32],
    vote_code: [u8; 32],
    auth_serial: [u8; 64],
    auth_code: [u8; 64],
    ack_code: [u8; 64],
    server_sign: [u8; 64])]
pub struct Accept<'info> {
    #[account(mut)]
    pub payer: Signer<'info>,
    #[account(mut)]
    pub user: Signer<'info>,

    #[account(
        init,
        seeds = [b"acceptVote".as_ref(), server_sign.as_ref()], // todo : poprawnie to policzyć a nie na palcach
        bump,
        payer = payer,
        space = 32 + 32 + 64 + 64 + 64 + 64 + 8)]
    pub cast: Account<'info, CastVote>,
    pub system_program: Program<'info, System>,
}

#[account]
pub struct AcceptVote {
    pub vote_serial: [u8; 32],
    pub vote_code: [u8; 32],
    pub auth_serial: [u8; 64],
    pub auth_code: [u8; 64],
    pub ack_code: [u8; 64],
    pub server_sign: [u8; 64],
    pub bump: u8,
}

#[derive(Accounts)]
#[instruction(vote_serial: [u8; 32],
    vote_code: [u8; 32],
    auth_serial: [u8; 64],
    auth_code: [u8; 64],
    ack_code: [u8; 64],
    server_sign: [u8; 64],
    voter_sign: [u8; 64])]
pub struct Commit<'info> {
    #[account(mut)]
    pub payer: Signer<'info>,
    #[account(mut)]
    pub user: Signer<'info>,

    #[account(
        init,
        seeds = [b"commitVote".as_ref(), voter_sign.as_ref()], // todo : poprawnie to policzyć a nie na palcach
        bump,
        payer = payer,
        space = 32 + 32 + 64+ 64 + 64 + 64 + 64 + 64 + 64 + 8
    )]
    pub commit: Account<'info, CommitVote>,
    pub system_program: Program<'info, System>,
}

#[account]
pub struct CommitVote {
    pub vote_serial: [u8; 32],
    pub vote_code: [u8; 32],
    pub auth_serial: [u8; 64],
    pub auth_code: [u8; 64],
    pub ack_code: [u8; 64],
    pub server_sign: [u8; 64],
    pub voter_sign: [u8; 64],
    pub bump: u8,
}