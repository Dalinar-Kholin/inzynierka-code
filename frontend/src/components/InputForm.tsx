interface InputForm {
    name: string;
    value: string;
    fn: (s: string) => void
}


export default function InputForm({name, fn, value}: InputForm) {
    return (
        <div>
            <p>{name}</p>
            <p>
                <input onChange={e => {
                    fn(e.target.value)
                }} value={value}/>
            </p>
        </div>
    )
}