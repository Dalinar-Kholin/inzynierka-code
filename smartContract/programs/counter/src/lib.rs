use anchor_lang::prelude::*;

declare_id!("5Me1semZqTJzDwrCR4qsBx1BMX5MfHdvNNqdNAGWBa56");

#[program]
mod counter {
    use super::*;

    pub fn first_stage_vote(
        ctx: Context<CreateAuthPackCommitment>,
        serial: [u8; 16],
        hashed_data: [u8; 32],
    ) -> Result<()> {
        let commitment_data = &mut ctx.accounts.commitment;
        commitment_data.serial = serial;
        commitment_data.hashed_data = hashed_data;
        commitment_data.bump = ctx.bumps.commitment;
        Ok(())
    }

    pub fn create_commitment_pack(
        ctx: Context<CreateAuthPackCommitment>,
        serial: [u8; 16],
        hashed_data: [u8; 32],
    ) -> Result<()> {
        let commitment_data = &mut ctx.accounts.commitment;
        commitment_data.serial = serial;
        commitment_data.hashed_data = hashed_data;
        commitment_data.bump = ctx.bumps.commitment;
        Ok(())
    }

    pub fn create(ctx: Context<Create>, index: u64, message: String) -> Result<()> {
        let account_data = &mut ctx.accounts.message_account;
        account_data.user = ctx.accounts.user.key();
        account_data.message = message;
        account_data.index = index;
        account_data.bump = ctx.bumps.message_account;
        Ok(())
    }
}

#[derive(Accounts)]
#[instruction(serial: [u8; 16], hashed_data : [u8; 32])]
pub struct CreateAuthPackCommitment<'info> {
    #[account(mut)]
    pub payer: Signer<'info>,

    #[account(
        init,
        payer = payer,
        space = 32 + 16 + 1 + 8, // todo : poprawn8PuBy6uMn4SRfDDZeJeuYH6hDE9eft1t791mFdUFc5Afie to policzyć a nie na palcach
        seeds = [b"createPackCommitment", serial.as_ref()],
        bump
    )]
    pub commitment: Account<'info, PackCommitment>,

    pub system_program: Program<'info, System>,
}

#[account]
pub struct PackCommitment {
    pub serial: [u8; 16],
    pub hashed_data: [u8; 32],
    pub bump: u8,
}

#[derive(Accounts)]
#[instruction(index: u64, message: String)]
pub struct Create<'info> {
    #[account(mut)]
    pub payer: Signer<'info>,
    #[account(mut)]
    pub user: Signer<'info>,

    #[account(
        init,
        seeds = [b"message", user.key().as_ref(), &index.to_le_bytes()],
        bump,
        payer = payer,
        space = 8 + 32 + 8 + 4 + message.len() + 1
    )]
    pub message_account: Account<'info, MessageAccount>,
    pub system_program: Program<'info, System>,
}

#[account]
pub struct MessageAccount {
    pub user: Pubkey,
    pub index: u64,
    pub message: String,
    pub bump: u8,
}
