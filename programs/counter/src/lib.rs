use anchor_lang::prelude::*;

declare_id!("8PuBy6uMn4SRfDDZeJeuYH6hDE9eft1t791mFdUFc5Af");

#[program]
mod counter {
    use super::*;

    pub fn create_commitment_auth_pack(
        ctx: Context<CreateAuthPackCommitment>,
        auth_serial : [u8; 16],
        encrypted_data : String,
    ) -> Result<()> {
        let commitment_data = &mut ctx.accounts.commitment;
        commitment_data.auth_serial = auth_serial;
        commitment_data.encrypted_data = encrypted_data;
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
#[instruction(auth_serial: [u8; 16], encrypted_data : String)]
pub struct CreateAuthPackCommitment<'info> {
    #[account(mut)]
    pub payer: Signer<'info>,

    #[account(
        init,
        payer = payer,
        space = encrypted_data.len() + 16 + 1 + 1 + 30, // todo : poprawnie to policzyć a nie na palcach
        seeds = [b"createAuthPackCommitment", auth_serial.as_ref()],
        bump
    )]
    pub commitment: Account<'info, AuthPackCommitment>,

    pub system_program: Program<'info, System>,
}

#[account]
pub struct AuthPackCommitment {
    pub auth_serial: [u8; 16],
    pub encrypted_data: String,
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
