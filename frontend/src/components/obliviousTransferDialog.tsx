import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import DialogTitle from '@mui/material/DialogTitle';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useTheme } from '@mui/material/styles';
import {Fragment, useState} from "react";

interface IResponsiveDialog {
    setUseFirstAuthCode: (v : boolean)=> void;
}

export default function ResponsiveDialog({setUseFirstAuthCode}: IResponsiveDialog) {
    const [open, setOpen] = useState(false);
    const theme = useTheme();
    const fullScreen = useMediaQuery(theme.breakpoints.down('md'));

    const handleClickOpen = () => {
        setOpen(true);
    };

    const handleClose = () => {
        setOpen(false);
    };

    return (
        <Fragment>
            <Button variant="outlined" onClick={handleClickOpen}>
                select which auth code get
            </Button>
            <Dialog
                fullScreen={fullScreen}
                open={open}
                onClose={handleClose}
                aria-labelledby="responsive-dialog-title"
            >
                <DialogTitle id="responsive-dialog-title">
                    {"Use Google's location service?"}
                </DialogTitle>
                <DialogContent>
                    <DialogContentText>
                        use first or second auth code
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    <Button onClick={()=>{
                        handleClose();
                        setUseFirstAuthCode(true)
                    }}>
                        First
                    </Button>
                    <Button onClick={()=>{
                        handleClose();
                        setUseFirstAuthCode(false)
                    }}>
                        Second
                    </Button>
                </DialogActions>
            </Dialog>
        </Fragment>
    );
}