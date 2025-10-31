
interface IGetAuthCode{
    sign: string,
    authSerial: string
}

export default async function getAuthCode({ authSerial } : IGetAuthCode){
    console.log(authSerial)
    const res =
        await fetch("http://127.0.0.1:8080/getAuthCode",{
            method: "POST"
        })
        .then(r => {
            if (!r.ok){
                throw new Error("esa")
            }
            return r
        }).then(r => r.json())
    return res
}