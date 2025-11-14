import ReactDOM from 'react-dom/client'
import './index.css'
import {BrowserRouter as Router} from "react-router-dom";
import RouterSwitcher from "./components/Router.tsx";
import {BallotProvider} from "./context/ballot/context.tsx";

ReactDOM.createRoot(document.getElementById('root')!).render(
    <Router>
        <BallotProvider>
            <RouterSwitcher />
        </BallotProvider>
    </Router>
)