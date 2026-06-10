<template>
	<div id="app">
		<router-view />
	</div>
</template>

<script>
	export default {
		name: 'App',
		watch: {},
		created() {
			//本地调试使用
			if (location.port || location.hostname == 'localhost' || /^(\d{1,3}\.){3}(\d{1,3})$/.test(location.hostname)) {
				//有端口运行的环境下 或localhost运行  及ip运行 直接返回当前url
				window.bjsdk.sJWT('Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IkQ2RThFNDc2MjE0NUY1NjZBMjg3QTMzNERENDZEMkVGRDQwNTU1RjJSUzI1NiIsInR5cCI6ImF0K2p3dCIsIng1dCI6IjF1amtkaUZGOVdhaWg2TTAzVWJTNzlRRlZmSSJ9.eyJuYmYiOjE3ODEwNzMxODMsImV4cCI6MTc4MTExNjM4MywiaXNzIjoiaHR0cHM6Ly93LTEudGVzdC5iZXRhd20uY29tIiwiYXVkIjoic2FtcGxlYXBpIiwiY2xpZW50X2lkIjoiYmV0YXZpcG1hbmFnZWNvcmVhdXRoYXBpIiwic3ViIjoiQkFBQUFBRUFBQUFZUURVQkE1S1gyRFE2IiwiYXV0aF90aW1lIjoxNzgxMDczMTgzLCJpZHAiOiJsb2NhbCIsImp0aSI6IjUyM0ExNUZDNDc2OUU5MjYwNjZFOEVBRDg5OThCQTBDIiwiaWF0IjoxNzgxMDczMTgzLCJzY29wZSI6WyJfdWRhIiwib3BlbmlkIiwic2FtcGxlYXBpIiwib2ZmbGluZV9hY2Nlc3MiXSwiYW1yIjpbImJldGFzYW1wbGUiXX0.iNvpSao86IxyaVBxeOTeV5X5MhHgzbUhgynnFfwH2ZxWlTQ-9zKQeobB5p5mbwkdLCUrJIpRexGt3u0uvOOlt1bBmRLXzmY2kOAbJROtwuD74cIVNG1QpmR2XNU5sRmDe-Le_VK4iht8tVdt94wqFdHBrVh5REsPnYNXbPBjppUjjbDcoR6sr114DaDNMbRV89SVHQdsQpFgq9NWOrsj7eBCobWHjEavP-LfXiq_wxaHi5bjobxiJfqRyLKjwJKps3Xz2Yf5Z8JhI82ygx5YAIIfk_-nYk6k-R834uwfVGRrsO4GNiW5-p-5j2Umrf5DEMNhSMqdFu6KZRm9O9A5lQ')
			}

		const _this = this
		//注册jsdk的config
		window.bjsdk.config({
			wxOps: {
				env: 'WW', //pc是否开启企微签名认证
			},
			authOps: {
				//使用新版授权
				authUrl: (window.bjsdk._ext.authOps() || {}).authUrl || '',
				useBearer: (window.bjsdk._ext.authOps() || {}).useBearer || '',
			},
			navOps: {
				pass: ['corpId', 'suiteId', 'uparms'], //ency--此项目加此配置是为了兼容早报后台
				confuse: 0, //goTo是否添加prp，0--不添加，1--随机添加，2--继承当前页面url
				reqAbsPath: false, //将url转换为绝对路径 //goTo方法重置
				nav(ops, navOps) {
					window.bjsdk._GoToCallBack(ops, navOps, _this.$router)
				},
			},
		})
	},
}
</script>

<style lang="less">
#app {
	font-family: 'Avenir', Helvetica, Arial, sans-serif;
	-webkit-font-smoothing: antialiased;
	-moz-osx-font-smoothing: grayscale;
	color: #2c3e50;
}
</style>
