const  bjsdk = window.bjsdk;

//请求头headers默认配置
const headersDef = {
    'Content-Type': 'application/json;charset=utf-8',
    'Accept': 'text/event-stream',
    'Authorization': bjsdk.gLS('bjwt'),
}
/**
 * 创建一个可取消的流式fetch请求。
 * @param {} data - 请求的URL、params：参数、method：请求方法、headers：请求头配置
 * @param {Function} callBack - 可选的回调函数，用于处理每个数据块。
 * @returns {ReadableStream} 返回一个可读流。
 */
export const fetchHttp = async ({url='',params=null, method='POST', headers={}},callBack=null) => {

    const controller = new AbortController(); // 创建一个 AbortController 实例
    const signal = controller.signal;//用于取消请求。
    typeof callBack === 'function' && callBack('',controller)// 调用回调函数处理数据块
    //执行fetch请求
    const response = await fetch(SetProxy(url), {
        method: method, 
        headers: {...headersDef,...headers},	
        body: params && JSON.stringify(params) ||'',
        signal
    });
    if (!response.ok) {
        controller.abort()
        typeof callBack === 'function' && callBack(JSON.stringify({type: 'error', message: '服务开小差了~'}),controller)// 调用回调函数处理数据块
        throw new Error(`HTTP error! response: ${JSON.stringify(response)}`);
    }
    const reader = response.body.getReader();
    const decoder = new TextDecoder("utf-8");
    let errorJsonStr = '';//保留上一个流式数据返回的非json字符串
    // 创建一个ReadableStream实例读取流式数据
    new ReadableStream({
        start(StreamController) {
            function push() {
                reader.read().then(({ done, value }) => {
                    console.log('done==',done)
                    if (done) {
                        StreamController.close();
                        typeof callBack === 'function' && callBack({ state: 'end' },controller)// 调用回调函数处理数据块
                        return;
                    }
                    const data = decoder.decode(value, { stream: true });
                    StreamController.enqueue(data);
                    console.log('stream data ==',new Date().getTime(),errorJsonStr+data)
                    const {result ,errorStr } = ResolveData(errorJsonStr+data);
                    console.log('result', result, errorStr)
                    errorJsonStr = errorStr;
                    typeof callBack === 'function' && callBack(result,controller)// 调用回调函数处理数据块
                    queueMicrotask(push);//执行一个微任务，循环读取
                }).catch(error => {
                    // StreamController.close();
                    // typeof callBack === 'function' && callBack({ state: 'end' }, null)// 调用回调函数处理数据块
                    throw new Error(`ReadableStream  error! StreamController: ${JSON.stringify(error)}`);
                });
            }
            push();
        }
		// start(StreamController) {
        //     function push() {
        //         reader.read().then(({ done, value }) => {
        //             console.log('done==',done)
        //             if (done) {
		// 				if (errorJsonStr) {
		// 					errorJsonStr.split('\ndata: ');
		// 					const ps = errorJsonStr.replace(/^data:\s*/, '').trim();
		// 					callBack([ps], controller);
		// 				}
        //                 StreamController.close();
        //                 // if (url.indexOf('AiTestRecord/record/reply') === -1) {
        //                 //     typeof callBack === 'function' && callBack({ state: 'end' },controller)// 调用回调函数处理数据块
        //                 // }
        //                 typeof callBack === 'function' && callBack({ state: 'end' },controller)// 调用回调函数处理数据块
        //                 return;
        //             }
        //             const data = decoder.decode(value, { stream: true });
		// 			errorJsonStr += data
		// 			const parts = errorJsonStr.split('\ndata: ');
		// 			errorJsonStr = parts.pop(); // 保留未完成的数据块
		// 			parts.forEach(part => {
		// 				part.split('\ndata: ');
		// 				const ps = part.replace(/^data:\s*/, '').trim();
		// 				callBack([ps], controller)
		// 			});
        //             StreamController.enqueue(value);
        //             push();//执行一个微任务，循环读取
        //         }).catch(error => {
        //             // StreamController.close();
        //             // typeof callBack === 'function' && callBack({ state: 'end' },controller)// 调用回调函数处理数据块
        //             throw new Error(`ReadableStream  error! StreamController: ${JSON.stringify(error)}`);
        //         });
        //     }
        //     push();
        // }
    });

    // 如果没有传入信号，则返回AbortController，以便外部可以调用其controller.abort();方法取消请求
    return { controller };
}
/**
 * 设置网络请求代理，支持本地测试环境
 * @param {*} url 
 * @returns 
 */
const SetProxy = (url='')=>{
    const path = url.split('?')[0].split('#')[0];
    if(!path.includes(location.origin)){
        url = bjsdk.url(`${location.origin}/rp/${url.replace(/^http(s)?:\/\//,'')}`,true);
    }
    return url.trim();
}
/**
 * 解析流式数据为数组对象
 * @param {*} data --流式返回数据
 * @returns {result:[],errorStr}  --result正确的数组对象   errorStr--错误的非json的字符串
 */
const ResolveData = (data) => {
    // const resArr = data.replace(/\s/g, '').split('data:')||[];
    const resArr = data.split('data:')||[];
    // const first = resArr.shift();
    // let last = resArr.pop();
    // if(IsJsonStr(first)) resArr.unshift(first) //处理第一个元素
    // if(IsJsonStr(last)){//处理最后一个元素
    //     resArr.push(last);
    //     last = '';
    // }
    // return {result:resArr,errorStr:last}
    const first = resArr[0];
    const length = resArr.length
    let last = resArr[length - 1];
    if(!IsJsonStr(first)) {
        resArr.shift();
    } //处理第一个元素
    if(IsJsonStr(last)){//处理最后一个元素
        last = '';
    } else {
        resArr.pop()
    }
    return {result:resArr,errorStr:last}
}
// const ResolveData = (data) => {
//     const results = [];
//     let index = data.indexOf('\n\n'); // 假设消息之间使用了两个换行符分隔
//     let newData = data
//     while (index > -1) {
//         const chunk = data.slice(0, index); // 提取一条完整的数据
//         newData = data.slice(index + 2); // 更新缓冲区
//         // 移除行首的 'data:'
//         const cleanedChunk = chunk.replace(/^data:\s*/, '');
//         // try {
//         //     const jsonData = JSON.parse(cleanedChunk);
//         //     results.push(cleanedChunk); // 解析成功，加入结果数组
//         // } catch (e) {
//         //     // JSON 解析失败，可能数据不完整，继续等待
//         //     newData = cleanedChunk + '\n\n' + newData; // 将未解析的数据放回缓冲区
//         //     break;
//         // }
//         if (IsJsonStr(cleanedChunk)) {
//             results.push(cleanedChunk)
//         } else {
//             newData = cleanedChunk + '\n\n' + newData;
//         }
//         index = newData.indexOf('\n\n'); // 查找下一个分隔符
//     }
//     return { result: results, errorStr: newData };
// }
/**
 * 判断字符串是否是json字符串
 * @param {*} str 
 * @returns Boolean
 */
function IsJsonStr(str) {  
    if (typeof str !== 'string' || str.trim() === '') {  
        return false;  
    }  
    try {  
        JSON.parse(str);  
        return true;  
    } catch (e) {  
        return false;  
    }  
}

