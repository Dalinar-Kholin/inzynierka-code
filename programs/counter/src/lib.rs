use anchor_lang::prelude::*;

declare_id!("Be4zMFBtAeH8mwgmuyAQjbdeXnSfh9PDQwk9Ac9SCfsN");

#[program]
mod counter {
    use super::*;

    pub fn create(ctx: Context<Create>, message: String) -> Result<()> {
        let account_data = &mut ctx.accounts.message_account;
        account_data.user = ctx.accounts.user.key();
        account_data.message = message;
        account_data.bump = ctx.bumps.message_account;
        Ok(())
    }

    pub fn update(ctx: Context<Update>, message: String) -> Result<()> {
        let account_data = &mut ctx.accounts.message_account;
        account_data.message = message;
        Ok(())
    }

    pub fn delete(_ctx: Context<Delete>) -> Result<()> {
        Ok(())
    }
}

#[derive(Accounts)]
#[instruction(message: String)]
pub struct Create<'info> {
    #[account(mut)]
    pub payer: Signer<'info>,
    #[account(mut)]
    pub user: Signer<'info>,

    #[account(
        init,
        seeds = [b"message", user.key().as_ref()],
        bump,
        payer = payer,
        space = 8 + 32 + 4 + message.len() + 1
    )]
    pub message_account: Account<'info, MessageAccount>,
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
#[instruction(message: String)]
pub struct Update<'info> {
    #[account(mut)]
    pub payer: Signer<'info>,
    #[account(mut)]
    pub user: Signer<'info>,

    #[account(
        mut,
        seeds = [b"message", user.key().as_ref()],
        bump = message_account.bump,
        realloc = 8 + 32 + 4 + message.len() + 1,
        realloc::payer = payer,
        realloc::zero = true,
    )]
    pub message_account: Account<'info, MessageAccount>,
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct Delete<'info> {
    #[account(mut)]
    pub user: Signer<'info>,

    #[account(
        mut,
        seeds = [b"message", user.key().as_ref()],
        bump = message_account.bump,
        close = user,
    )]
    pub message_account: Account<'info, MessageAccount>,
    pub system_program: Program<'info, System>,
}

#[account]
pub struct MessageAccount {
    pub user: Pubkey,
    pub message: String,
    pub bump: u8,
}
