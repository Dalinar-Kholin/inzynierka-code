use anchor_lang::prelude::*;

declare_id!("8PuBy6uMn4SRfDDZeJeuYH6hDE9eft1t791mFdUFc5Af");

const VOTE_CODE_LENGTH: usize = 3;
const AUTH_CODE_CODE_LENGTH: usize = 64;

#[derive(AnchorSerialize, AnchorDeserialize, Clone, Copy, Debug, PartialEq, Eq)]
pub enum VotingStage {
    Empty,
    Casted,
    Accepted,
    Committed,
}

#[program]
mod counter {
    use anchor_lang::__private::bytemuck::cast;
    use super::*;

    pub fn cast_vote( // do umieszczenia krotki <VoteCode,AuthCode> na BB
                      ctx: Context<CastCtx>,
                      auth_code: Vec<u8>,
                      vote_serial: Vec<u8>,
                      vote_code: Vec<u8>,
                      lock_code: Vec<u8>,
    ) -> Result<()> {
        let cast = &mut ctx.accounts.vote;
        require!(cast.stage == VotingStage::Empty, ErrorCode::InvalidProgramId);

        require!(auth_code.len() == 64, ErrorCode::InvalidProgramId);
        require!(vote_serial.len() == 16, ErrorCode::InvalidProgramId);
        require!(vote_code.len() == 3, ErrorCode::InvalidProgramId);
        require!(lock_code.len() == 8, ErrorCode::InvalidProgramId);

        let mut auth_fixed = [0u8; 64];
        auth_fixed.copy_from_slice(&auth_code);

        let mut serial_fixed = [0u8; 16];
        serial_fixed.copy_from_slice(&vote_serial);

        let mut vote_fixed = [0u8; 3];
        vote_fixed.copy_from_slice(&vote_code);

        let mut lock_fixed = [0u8; 8];
        lock_fixed.copy_from_slice(&lock_code);

        cast.vote_serial = serial_fixed;
        cast.auth_code = auth_fixed;
        cast.vote_code = vote_fixed;
        cast.lock_code = lock_fixed;
        cast.bump = ctx.bumps.vote;
        cast.stage = VotingStage::Casted;
        Ok(())
    }

    pub fn accept_vote(
        ctx: Context<AcceptCtx>,
        auth_code: Vec<u8>,
        auth_serial: Vec<u8>,
        server_sign: Vec<u8> // po co ack code skoro mamy podpis
    ) -> Result<()> { // do umieszczenia krotki <VoteSerial, VoteCode, AuthSerial, AuthCode, sig(servera)> na BB
        let cast = &mut ctx.accounts.vote;
        require!(cast.stage == VotingStage::Casted, ErrorCode::InvalidProgramId);

        let mut auth_fixed = [0u8; 16];
        auth_fixed.copy_from_slice(&auth_serial);
        let mut server_fixed = [0u8; 64];
        server_fixed.copy_from_slice(&server_sign);

        cast.auth_serial = auth_fixed;
        cast.server_sign = server_fixed;

        cast.stage = VotingStage::Accepted;
        Ok(())
    }

    pub fn commit_vote(
        ctx: Context<CommitCtx>,
        auth_code: Vec<u8>,
        offset: u32,
        user_sign: Vec<u8>
    ) -> Result<()> { // do umieszczenia krotki <VoteSerial, VoteCode, AuthSerial, AuthCode, sig(servera)> na BB
        let vote = &mut ctx.accounts.vote;

        let offset = offset as usize;
        let end = offset
            .checked_add(user_sign.len())
            .ok_or(ErrorCode::InvalidProgramId)?;
        require!(end <= MAX_VOTER_SIGN_LEN, ErrorCode::InvalidProgramId);

        vote.voter_sign.resize(end, 0);

        vote.voter_sign[offset..end].copy_from_slice(&user_sign);

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

    pub fn create_key(
        ctx: Context<CreateSignKey>,
        key: [u8; 113],
    ) -> Result<()> {
        let commitment_data = &mut ctx.accounts.key;
        commitment_data.key = key;
        commitment_data.bump = ctx.bumps.key;
        Ok(())
    }

    pub fn create_single_commitment(
        ctx: Context<CreateSingleCommitment>,
        commitment_type: u8,
        id: u8,
        to_commit: [u8; 64],
    ) -> Result<()> {
        let commitment_data = &mut ctx.accounts.commitment;
        commitment_data.commitment_type = commitment_type;
        commitment_data.id = id;
        commitment_data.to_commit = to_commit;
        commitment_data.bump = ctx.bumps.commitment;
        Ok(())
    }
}
    #[derive(Accounts)]
    #[instruction(commitment_type: u8, id: u8, to_commit: [u8; 64])]
    pub struct CreateSingleCommitment<'info> {
        #[account(mut)]
        pub payer: Signer<'info>,

        #[account(
        init,
        payer = payer,
        space = 8 + 1 + 1 + 64 + 1, // 8 = discriminator
        seeds = [
            b"createSingleCommitment".as_ref(), &[commitment_type], &[id],
        ],
        bump
        )]
        pub commitment: Account<'info, SingleCommitment>,

        pub system_program: Program<'info, System>,
    }

    #[account]
    pub struct SingleCommitment {
        pub commitment_type: u8,
        pub id: u8,
        pub to_commit: [u8; 64],
        pub bump: u8,
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
#[instruction(key : [u8; 32])]
pub struct CreateSignKey<'info> {
    #[account(mut)]
    pub payer: Signer<'info>,

    #[account(
        init,
        payer = payer,
        space = 114 + 1 + 8, // todo : poprawnie to policzyć a nie na palcach
        seeds = [b"signKey"], // aby przy próbie nadpisania klucza rzucało błędem
        bump
    )]
    pub key: Account<'info, SignKey>,

    pub system_program: Program<'info, System>,
}

#[account]
pub struct SignKey {
    pub key: [u8; 113],
    pub bump: u8,
}


#[derive(Accounts)]
#[instruction(auth_code: Vec<u8>)]
pub struct CastCtx<'info> {
    #[account(mut)]
    pub payer: Signer<'info>,
    #[account(
        init,
        payer = payer,
        seeds = [b"commitVote", &auth_code[..32], &auth_code[32..]],
        bump,
        space = VOTE_ACCOUNT_SPACE,
    )]
    pub vote: Account<'info, Vote>,
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
#[instruction(auth_code: Vec<u8>)]
pub struct AcceptCtx<'info> {
    #[account(
        mut,
        seeds = [b"commitVote", &auth_code[..32], &auth_code[32..]],
        bump = vote.bump
    )]
    pub vote: Account<'info, Vote>,
}

#[derive(Accounts)]
#[instruction(auth_code: Vec<u8>)]
pub struct CommitCtx<'info> {
    #[account(
        mut,
        seeds = [b"commitVote", &auth_code[..32], &auth_code[32..]],
        bump = vote.bump,
    )]
    pub vote: Account<'info, Vote>,
}

#[account]
pub struct Vote {
    pub stage: VotingStage,
    pub vote_serial: [u8; 16],
    pub vote_code: [u8; VOTE_CODE_LENGTH],
    pub auth_serial: [u8; 16],
    pub auth_code: [u8; AUTH_CODE_CODE_LENGTH],
    pub server_sign: [u8; 64],
    pub lock_code: [u8; 8],
    pub voter_sign: Vec<u8>,
    // vote vector --> po wymnozeniu wszystkich mamy wynik
    pub bump: u8,
}
const MAX_VOTER_SIGN_LEN: usize = 5000;

const VOTE_ACCOUNT_SPACE: usize =
    8    // discriminator
        + 1    // stage
        + 16   // vote_serial
        + 3    // vote_code
        + 16   // auth_serial
        + 64   // auth_code
        + 64   // server_sign
        + 4    // voter_sign length prefix (u32)
        + 8   // lock_code
        + MAX_VOTER_SIGN_LEN
        + 1;   // bump
