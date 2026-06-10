module.exports = {
	presets: ["@vue/cli-plugin-babel/preset"],
	plugins: [
		"@babel/plugin-proposal-optional-chaining", // 可选链语法转义：a?.b
		"@babel/plugin-proposal-nullish-coalescing-operator", // 双问号语法转义：a?.b ?? []
	]
};