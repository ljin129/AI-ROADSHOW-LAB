const xmlHttp = (url, par, method = 'post', callBack) => {
	// 创建一个新的 XMLHttpRequest 对象
	const xhr = new XMLHttpRequest();
	// 设置请求方法为 "POST"，并提供目标 URL
	xhr.open(method, url, true);
	// 设置请求头以指定 Content-Type 和 Accept 类型
	xhr.setRequestHeader("Content-Type", "application/json;charset=UTF-8");
	xhr.setRequestHeader("Accept", "text/event-stream");
	// 定义 onprogress 事件处理程序以实时处理服务器响应
	xhr.onprogress = function () {
		if (xhr.status == 200 && xhr.responseText) {
			callBack(xhr.responseText);
		} else {
			callBack({ state: 'error' });
		}
	};
	// 定义 onload 和 onerror 事件处理程序以处理请求完成或出错的情况
	xhr.onload = function () {
		if (xhr.status !== 200) {
			console.error("Error: ", xhr.statusText);
			callBack({ state: 'error' });
		}
	};
	xhr.onloadend = function () {
		if (xhr.status == 200) callBack({ state: 'end' });
	};
	xhr.onerror = function () {
		console.error("Request failed");
		callBack({ state: 'error' });
	};
	// 发送请求
	xhr.send(JSON.stringify(par));
}

export { xmlHttp }
